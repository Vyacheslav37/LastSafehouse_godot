package com.mygame.lastsafe;

import android.app.Activity;
import android.util.Log;
import android.view.View;

import androidx.annotation.NonNull;

import com.yandex.mobile.ads.common.MobileAds;
import com.yandex.mobile.ads.interstitial.InterstitialAd;
import com.yandex.mobile.ads.interstitial.InterstitialAdEventListener;
import com.yandex.mobile.ads.interstitial.InterstitialAdError;

import org.godotengine.godot.Godot;
import org.godotengine.godot.plugin.GodotPlugin;
import org.godotengine.godot.plugin.SignalInfo;

import java.util.Collections;
import java.util.List;

public class LastSafeYandexAds extends GodotPlugin {

    private InterstitialAd interstitialAd;
    private Activity activity;

    public LastSafeYandexAds(Godot godot) {
        super(godot);
        this.activity = godot.getActivity();
    }

    @NonNull
    @Override
    public String getPluginName() {
        return "LastSafeYandexAds";
    }

    @Override
    public View onMainCreate(Activity activity) {
        // Инициализация Yandex SDK
        MobileAds.initialize(activity, initializationStatus -> {
            Log.i("LastSafeYandexAds", "MobileAds initialized");
        });
        return null;
    }

    @NonNull
    @Override
    public List<SignalInfo> getPluginSignals() {
        return Collections.unmodifiableList(
            List.of(
                new SignalInfo("interstitial_loaded"),
                new SignalInfo("interstitial_failed", String.class),
                new SignalInfo("interstitial_dismissed")
            )
        );
    }

    // ----------------------
    // Методы для GDScript
    // ----------------------

    public void testLog() {
        Log.i("LastSafeYandexAds", "testLog() called from Godot");
    }

    public void loadInterstitial(final String blockId) {
        activity.runOnUiThread(() -> {
            if (interstitialAd == null) {
                interstitialAd = new InterstitialAd(activity);
                interstitialAd.setBlockId(blockId);
                interstitialAd.setInterstitialAdEventListener(new InterstitialAdEventListener() {
                    @Override
                    public void onAdLoaded() {
                        Log.i("LastSafeYandexAds", "Interstitial loaded");
                        emitSignal("interstitial_loaded");
                    }

                    @Override
                    public void onAdFailedToLoad(@NonNull InterstitialAdError interstitialAdError) {
                        String error = interstitialAdError.getDescription();
                        Log.e("LastSafeYandexAds", "Interstitial failed: " + error);
                        emitSignal("interstitial_failed", error);
                    }

                    @Override
                    public void onAdShown() {
                        Log.i("LastSafeYandexAds", "Interstitial shown");
                    }

                    @Override
                    public void onAdDismissed() {
                        Log.i("LastSafeYandexAds", "Interstitial dismissed");
                        emitSignal("interstitial_dismissed");
                    }

                    @Override
                    public void onAdClicked() {}
                    @Override
                    public void onLeftApplication() {}
                });
            }

            interstitialAd.loadAd();
        });
    }

    public void showInterstitial() {
        activity.runOnUiThread(() -> {
            if (interstitialAd != null && interstitialAd.isLoaded()) {
                interstitialAd.show(activity);
            } else {
                Log.w("LastSafeYandexAds", "Interstitial not loaded yet");
            }
        });
    }

    public boolean isInterstitialLoaded() {
        return interstitialAd != null && interstitialAd.isLoaded();
    }
}
