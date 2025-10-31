package com.mygame.LastSafehouse.yandexinterstitial;

import android.app.Activity;
import android.util.Log;

import org.godotengine.godot.Godot;
import org.godotengine.godot.plugin.GodotPlugin;
import org.godotengine.godot.plugin.SignalInfo;

import com.yandex.mobile.ads.AdRequest;
import com.yandex.mobile.ads.MobileAds;
import com.yandex.mobile.ads.common.AdError;
import com.yandex.mobile.ads.common.FullScreenContentCallback;
import com.yandex.mobile.ads.interstitial.InterstitialAd;
import com.yandex.mobile.ads.interstitial.InterstitialAdLoadCallback;
import com.yandex.mobile.ads.interstitial.InterstitialAdLoadError;

import java.util.HashSet;
import java.util.Set;
import java.util.concurrent.atomic.AtomicBoolean;

public class YandexInterstitialPlugin extends GodotPlugin {

    private static final String TAG = "YandexInterstitial";
    private InterstitialAd interstitialAd = null;
    private final AtomicBoolean isLoading = new AtomicBoolean(false);
    private boolean initialized = false;

    public YandexInterstitialPlugin(Godot godot) {
        super(godot);
    }

    @Override
    public String getPluginName() {
        return "YandexInterstitial";
    }

    @Override
    public Set<SignalInfo> getPluginSignals() {
        Set<SignalInfo> signals = new HashSet<>();
        signals.add(new SignalInfo("onInterstitialLoaded"));
        signals.add(new SignalInfo("onInterstitialError", String.class));
        signals.add(new SignalInfo("onInterstitialClosed"));
        signals.add(new SignalInfo("onInterstitialShown"));
        signals.add(new SignalInfo("onDebugMessage", String.class));
        return signals;
    }

    // -------------------------
    // Новый метод для проверки package name
    // -------------------------
    @Export
    public String getPackageName() {
        Activity activity = getActivity();
        if (activity != null) {
            return activity.getPackageName();
        } else {
            return "Activity not available";
        }
    }

    // -------------------------
    // Инициализация SDK
    // -------------------------
    public void init() {
        if (initialized) {
            emitSignal("onDebugMessage", "✅ Yandex SDK already initialized");
            return;
        }

        Activity activity = getActivity();
        if (activity == null || activity.isFinishing() || activity.isDestroyed()) {
            emitSignal("onInterstitialError", "Activity is not available");
            return;
        }

        activity.runOnUiThread(() -> {
            try {
                MobileAds.initialize(activity, () -> {
                    initialized = true;
                    emitSignal("onDebugMessage", "✅ Yandex SDK initialized");
                });
            } catch (Exception e) {
                emitSignal("onInterstitialError", "Initialization failed: " + e.getMessage());
            }
        });
    }

    // -------------------------
    // Загрузка интерстициальной рекламы
    // -------------------------
    public void loadInterstitial(String blockId) {
        if (!initialized) {
            emitSignal("onInterstitialError", "SDK not initialized. Call init() first.");
            return;
        }
        if (isLoading.get()) {
            emitSignal("onInterstitialError", "Load in progress");
            return;
        }

        Activity activity = getActivity();
        if (activity == null || activity.isFinishing() || activity.isDestroyed()) {
            emitSignal("onInterstitialError", "Activity is not available");
            return;
        }

        isLoading.set(true);
        interstitialAd = null;

        activity.runOnUiThread(() -> {
            InterstitialAd.load(
                activity,
                blockId,
                new AdRequest.Builder().build(),
                new InterstitialAdLoadCallback() {
                    @Override
                    public void onLoaded(InterstitialAd ad) {
                        isLoading.set(false);
                        interstitialAd = ad;
                        emitSignal("onInterstitialLoaded");

                        ad.setFullScreenContentCallback(new FullScreenContentCallback() {
                            @Override
                            public void onAdShowedFullScreenContent() {
                                emitSignal("onInterstitialShown");
                            }

                            @Override
                            public void onAdDismissedFullScreenContent() {
                                emitSignal("onInterstitialClosed");
                            }

                            @Override
                            public void onAdFailedToShowFullScreenContent(AdError error) {
                                emitSignal("onInterstitialError", error.getDescription());
                            }
                        });
                    }

                    @Override
                    public void onError(InterstitialAdLoadError error) {
                        isLoading.set(false);
                        emitSignal("onInterstitialError", "❌ Load error: " + error.getError().getDescription());
                    }
                }
            );
        });
    }

    // -------------------------
    // Показ интерстициальной рекламы
    // -------------------------
    public void showInterstitial() {
        if (interstitialAd != null) {
            Activity activity = getActivity();
            if (activity == null || activity.isFinishing() || activity.isDestroyed()) {
                emitSignal("onInterstitialError", "Activity is not available");
                return;
            }
            activity.runOnUiThread(() -> interstitialAd.show(activity));
        } else {
            emitSignal("onInterstitialError", "No ad loaded");
        }
    }

    @Override
    public void onMainDestroy() {
        interstitialAd = null;
    }
}
