extends Node

signal interstitial_loaded
signal interstitial_failed(error: String)
signal interstitial_dismissed

var _plugin = null

func _ready():
	# Всё условие должно быть внутри функции
	if Engine.has_singleton("LastSafeYandexAds"):
		_plugin = Engine.get_singleton("LastSafeYandexAds")
		_plugin.connect("interstitial_loaded", self, "_on_loaded")
		_plugin.connect("interstitial_failed", self, "_on_failed")
		_plugin.connect("interstitial_dismissed", self, "_on_dismissed")
	else:
		push_warning("LastSafeYandexAds: плагин не найден (нормально на ПК)")

# Java → GDScript
func _on_loaded():
	emit_signal("interstitial_loaded")

func _on_failed(error):
	emit_signal("interstitial_failed", error)

func _on_dismissed():
	emit_signal("interstitial_dismissed")

# Обёртка над Java-методами
func load_interstitial(id: String):
	if _plugin:
		_plugin.loadInterstitial(id)

func show_interstitial():
	if _plugin:
		_plugin.showInterstitial()

func is_loaded() -> bool:
	return _plugin and _plugin.isInterstitialLoaded()
