// res://android/plugins/LastSafeYandexAds/src/main/java/com/mygame/lastsafe/LastSafeYandexAds.kt
package com.mygame.lastsafe

import org.godotengine.godot.GodotPlugin
import android.app.Activity
import com.yandex.mobile.ads.common.MobileAds

class LastSafeYandexAds : GodotPlugin() {
    override fun getPluginName() = "LastSafeYandexAds"

    override fun onMainCreate(activity: Activity?) {
        activity?.let {
            MobileAds.initialize(it) {
                println("[Yandex Ads] Yandex Mobile Ads 7.16.1 initialized successfully")
            }
            println("[Yandex Ads] Yandex Mobile Ads 7.16.1 integrated successfully")
        }
    }
}