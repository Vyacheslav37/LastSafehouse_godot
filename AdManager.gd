# AdManager.gd
extends Node

signal reward_granted(amount: int)  # ← ЭТОТ СИГНАЛ!

func _ready():
	if OS.get_name() == "Android":
		YandexSDK.init()
		YandexSDK.setAdBlockId("demo-interstitial-yandex")  # ← ТВОЙ РЕАЛЬНЫЙ ID!
		#YandexSDK.setAdBlockId("R-M-XXXXXX-1")  # ← ТВОЙ РЕАЛЬНЫЙ ID!

		if not YandexSDK.is_connected("showRewardedVideoClosed", _on_rewarded_closed):
			YandexSDK.connect("showRewardedVideoClosed", _on_rewarded_closed)

func show_rewarded():
	if Engine.has_singleton("YandexSDK"):
		YandexSDK.showRewardedVideo()

func show_interstitial():
	if Engine.has_singleton("YandexSDK"):
		YandexSDK.showFullscreenAdv()

func _on_rewarded_closed(success: bool):
	if success:
		reward_granted.emit(3)  # ← +3 ПАТРОНА!
		
		#C:\Program Files\Eclipse Adoptium\jdk-17.0.16.8-hotspot
#		| Тип рекламы                             | Тестовый ID                |
#| --------------------------------------- | -------------------------- |
#| Баннер                                  | `demo-banner-yandex`       |
#| Интерстициал (полноэкранное объявление) | `demo-interstitial-yandex` |
#| Вознаграждаемое видео                   | `demo-rewarded-yandex`     |
#| Native-объявление                       | `demo-native-yandex`       |
