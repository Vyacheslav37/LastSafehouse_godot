package org.yourcompany.yandexads;

import org.godotengine.godot.Godot;
import org.godotengine.godot.plugin.GodotPlugin;
import java.util.Collections;
import java.util.Set;

public class LastSafeYandexAds extends GodotPlugin {
    public LastSafeYandexAds(Godot godot) {
        super(godot);
    }

    @Override
    public String getPluginName() {
        return "LastSafeYandexAds";
    }

    @Override
    public Set<String> getPluginMethods() {
        return Set.of("loadInterstitial", "showInterstitial", "isInterstitialLoaded");
    }

    // Примеры методов
    public void loadInterstitial(String id) {
        // Загрузка рекламы
    }

    public void showInterstitial() {
        // Показ рекламы
    }

    public boolean isInterstitialLoaded() {
        return true; // или false, в зависимости от состояния
    }
}
