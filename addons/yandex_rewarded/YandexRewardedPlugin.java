package com.godot.game;

import android.app.Activity;
import android.util.Log;

import androidx.annotation.NonNull;
import androidx.annotation.Nullable;

import com.yandex.mobile.ads.common.AdError;
import com.yandex.mobile.ads.common.ImpressionData;
import com.yandex.mobile.ads.rewarded.Reward;
import com.yandex.mobile.ads.rewarded.RewardedAd;
import com.yandex.mobile.ads.rewarded.RewardedAdEventListener;
import com.yandex.mobile.ads.rewarded.RewardedAdLoadListener;
import com.yandex.mobile.ads.rewarded.RewardedAdLoader;

import org.godotengine.godot.Godot;
import org.godotengine.godot.plugin.GodotPlugin;
import org.godotengine.godot.plugin.SignalInfo;
import org.godotengine.godot.plugin.UsedByGodot;

import java.util.HashSet;
import java.util.Set;

public class YandexRewardedPlugin extends GodotPlugin {

    private static final String TAG = "YandexRewarded";
    private RewardedAd rewardedAd = null;
    private boolean adLoaded = false;

    public YandexRewardedPlugin(Godot godot) {
        super(godot);
    }

    @NonNull
    @Override
    public String getPluginName() {
        return "YandexRewarded";
    }

    @Override
    public Set<SignalInfo> getPluginSignals() {
        Set<SignalInfo> signals = new HashSet<>();
        signals.add(new SignalInfo("onRewardedLoaded"));
        signals.add(new SignalInfo("onRewardedError", String.class));
        signals.add(new SignalInfo("onRewardedShown"));
        signals.add(new SignalInfo("onRewardedClosed"));
        signals.add(new SignalInfo("onRewardedGranted"));
        signals.add(new SignalInfo("onDebugMessage", String.class)); // Для GDScript-логов
        return signals;
    }

    private void debug(String message) {
        Log.d(TAG, message);
        emitSignal("onDebugMessage", "[YANDEX] " + message);
    }

    @UsedByGodot
    public void loadRewarded(String blockId) {
        debug("loadRewarded called | blockId: " + blockId);

        Activity activity = getActivity();
        if (activity == null) {
            debug("loadRewarded failed: activity is null");
            emitSignal("onRewardedError", "Activity unavailable");
            return;
        }

        adLoaded = false;
        rewardedAd = null;

        activity.runOnUiThread(() -> {
            RewardedAdLoader loader = new RewardedAdLoader.Builder(activity, blockId)
                .withAdLoadListener(new RewardedAdLoadListener() {
                    @Override
                    public void onAdLoaded(@NonNull RewardedAd ad) {
                        rewardedAd = ad;
                        adLoaded = true;
                        debug("✅ onAdLoaded: ad ready");
                        emitSignal("onRewardedLoaded");
                    }

                    @Override
                    public void onAdFailedToLoad(@NonNull AdError error) {
                        rewardedAd = null;
                        adLoaded = false;
                        debug("❌ onAdFailedToLoad: code=" + error.getCode() + ", desc=\"" + error.getDescription() + "\"");
                        emitSignal("onRewardedError", error.getDescription());
                    }
                })
                .build();

            debug("Starting ad load...");
            loader.loadAd();
        });
    }

    @UsedByGodot
    public void showRewarded() {
        debug("showRewarded called");

        Activity activity = getActivity();
        if (activity == null) {
            debug("showRewarded failed: activity is null");
            emitSignal("onRewardedError", "Activity unavailable");
            return;
        }

        activity.runOnUiThread(() -> {
            if (rewardedAd != null && adLoaded) {
                debug("Showing rewarded ad...");
                rewardedAd.show(activity, new RewardedAdEventListener() {
                    @Override
                    public void onAdImpression(@Nullable ImpressionData impressionData) {
                        // Обязательный метод в SDK 6.7.0
                    }

                    @Override
                    public void onAdClicked() {
                        debug("🖱️ onAdClicked");
                    }

                    @Override
                    public void onAdShown() {
                        debug("👁️ onAdShown");
                        emitSignal("onRewardedShown");
                    }

                    @Override
                    public void onAdDismissed() {
                        debug("🚪 onAdDismissed");
                        emitSignal("onRewardedClosed");
                        rewardedAd = null;
                        adLoaded = false;
                    }

                    @Override
                    public void onRewarded(@NonNull Reward reward) {
                        debug("🎁 onRewarded: amount=" + reward.getAmount() + ", type=" + reward.getType());
                        emitSignal("onRewardedGranted");
                    }

                    @Override
                    public void onAdError(@NonNull AdError error) {
                        debug("🔥 onAdError: code=" + error.getCode() + ", desc=\"" + error.getDescription() + "\"");
                        emitSignal("onRewardedError", error.getDescription());
                        rewardedAd = null;
                        adLoaded = false;
                    }
                });
            } else {
                String msg = "showRewarded failed: ad not loaded (rewardedAd=" + (rewardedAd != null) + ", adLoaded=" + adLoaded + ")";
                debug(msg);
                emitSignal("onRewardedError", "Ad not loaded");
            }
        });
    }

    @UsedByGodot
    public boolean isLoaded() {
        boolean result = (rewardedAd != null) && adLoaded;
        debug("isLoaded() → " + result);
        return result;
    }
}