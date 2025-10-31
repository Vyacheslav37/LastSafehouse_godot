package com.yourdomain.yandexrewarded;

import com.godot.game.Godot;
import com.godot.game.GodotPlugin;
import com.yandex.mobile.ads.AdRequest;
import com.yandex.mobile.ads.MobileAds;
import com.yandex.mobile.ads.rewarded.RewardedAd;
import com.yandex.mobile.ads.rewarded.RewardedAdLoadCallback;
import com.yandex.mobile.ads.rewarded.RewardedAdLoadError;

import java.util.concurrent.atomic.AtomicBoolean;

public class YandexRewardedPlugin extends GodotPlugin {

    private RewardedAd rewardedAd = null;
    private final AtomicBoolean isLoading = new AtomicBoolean(false);

    public YandexRewardedPlugin(Godot godot) {
        super(godot);
    }

    @Override
    public String getPluginName() {
        return "YandexRewarded"; // ← Имя синглтона в GDScript
    }

    @Override
    public void onMainActivityResult(int requestCode, int resultCode, android.content.Intent data) {}

    public void initialize() {
        if (!MobileAds.isInitialized()) {
            emitSignal("onDebugMessage", "Initializing Yandex SDK...");
            MobileAds.initialize(getActivity(), () -> {
                emitSignal("onDebugMessage", "✅ Yandex SDK initialized");
            });
        } else {
            emitSignal("onDebugMessage", "✅ Already initialized");
        }
    }

    public void loadRewarded(String blockId) {
        if (isLoading.get()) {
            emitSignal("onRewardedError", "Load in progress");
            return;
        }

        isLoading.set(true);
        rewardedAd = null;

        RewardedAd.load(
            getActivity(),
            blockId,
            new AdRequest.Builder().build(),
            new RewardedAdLoadCallback() {
                @Override
                public void onLoaded(RewardedAd ad) {
                    isLoading.set(false);
                    rewardedAd = ad;
                    emitSignal("onRewardedLoaded");

                    ad.setFullScreenContentCallback(new com.yandex.mobile.ads.common.FullScreenContentCallback() {
                        @Override
                        public void onAdDismissedFullScreenContent() {
                            emitSignal("onRewardedClosed");
                        }
                        @Override
                        public void onAdFailedToShowFullScreenContent(com.yandex.mobile.ads.common.AdError error) {
                            emitSignal("onRewardedError", error.getDescription());
                        }
                    });

                    ad.setRewardedAdEventListener(() -> {
                        emitSignal("onRewardedGranted");
                    });

                    emitSignal("onDebugMessage", "✅ Rewarded ad loaded");
                }

                @Override
                public void onError(RewardedAdLoadError error) {
                    isLoading.set(false);
                    emitSignal("onRewardedError", error.getError().getDescription());
                    emitSignal("onDebugMessage", "❌ Load error: " + error.getError().getDescription());
                }
            }
        );
    }

    public void showRewarded() {
        if (rewardedAd != null) {
            rewardedAd.show(getActivity());
            emitSignal("onDebugMessage", "▶️ Showing rewarded ad");
        } else {
            emitSignal("onRewardedError", "No ad loaded");
            emitSignal("onDebugMessage", "⚠️ No ad to show");
        }
    }

    @Override
    public String[] getPluginMethods() {
        return new String[]{"initialize", "loadRewarded", "showRewarded"};
    }

    @Override
    public void onMainDestroy() {
        rewardedAd = null;
    }
}