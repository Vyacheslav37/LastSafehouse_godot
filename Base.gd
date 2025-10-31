#R-M-17620744-1
extends Node2D

const MESSAGE_DURATION := 2.0
# Используйте тестовый ID при разработке!
const YANDEX_INTERSTITIAL_ID := "R-M-DEMO-interstitial"
# Перед публикацией замените на: const YANDEX_INTERSTITIAL_ID := "R-M-17620744-1"

@onready var food_label = get_node_or_null("CanvasLayer/InfoBackground/VBoxContainer/FoodLabel")
@onready var meds_label = get_node_or_null("CanvasLayer/InfoBackground/VBoxContainer/MedsLabel")
@onready var ammo_label = get_node_or_null("CanvasLayer/InfoBackground/VBoxContainer/AmmoLabel")
@onready var metal_label = get_node_or_null("CanvasLayer/InfoBackground/VBoxContainer/MetalLabel")
@onready var fuel_label = get_node_or_null("CanvasLayer/InfoBackground/VBoxContainer/FuelLabel")
@onready var water_label = get_node_or_null("CanvasLayer/InfoBackground/VBoxContainer/WaterLabel")
@onready var base_hp_label = get_node_or_null("CanvasLayer/InfoBackground/VBoxContainer/BaseHPLabel")
@onready var survivors_label = get_node_or_null("CanvasLayer/InfoBackground/VBoxContainer/SurvivorsLabel")

@onready var hover_sound = preload("res://sounds/hover_click.ogg")
@onready var click_sound = preload("res://sounds/item_click.ogg")

var sfx_hover: AudioStreamPlayer
var sfx_click: AudioStreamPlayer
var _current_message: Control = null
var _message_timer: SceneTreeTimer = null
var _is_ad_in_progress := false
var _is_sdk_ready := false

# Укажи package name, который зарегистрирован в Yandex Ads
const YANDEX_PACKAGE_NAME := "com.mygame.LastSafehouse"

func check_package_name():
	var device_package := ""
	
	if OS.has_feature("Android") or OS.has_feature("iOS"):
		if Engine.has_singleton("YandexInterstitial"):
			var ads = Engine.get_singleton("YandexInterstitial")
			# Вызываем метод из Java-плагина (Android) или аналогичный для iOS
			device_package = ads.getPackageName()
		else:
			print("❌ Синглтон YandexInterstitial не найден. Плагин не активен или платформа не поддерживается.")
			show_message("❌ YandexInterstitial не найден. Реклама не будет работать.", 4.0)
			return
	else:
		# ПК/редактор: ставим фиктивное имя, чтобы код не падал
		device_package = "org.godotengine.editor"

	if device_package != YANDEX_PACKAGE_NAME:
		print("❌ Package name не совпадает с Yandex Ads!")
		print("   Текущий:", device_package)
		print("   Ожидаемый:", YANDEX_PACKAGE_NAME)
		show_message("❌ Package name не совпадает с Yandex Ads!", 4.0)
	else:
		print("✅ Package name совпадает с Yandex Ads:", device_package)


func _ready():
	update_labels()
	check_package_name()
	sfx_hover = AudioStreamPlayer.new()
	sfx_hover.name = "SFX_Hover"
	add_child(sfx_hover)
	sfx_click = AudioStreamPlayer.new()
	sfx_click.name = "SFX_Click"
	add_child(sfx_click)

	var dummy_player = AudioStreamPlayer.new()
	dummy_player.stream = hover_sound
	dummy_player.name = "AudioDummy"
	add_child(dummy_player)

	for area in get_tree().get_nodes_in_group("interactables"):
		if not area.is_connected("mouse_entered", Callable(self, "_on_area_hover")):
			area.connect("mouse_entered", Callable(self, "_on_area_hover").bind(area, true))
		if not area.is_connected("mouse_exited", Callable(self, "_on_area_hover")):
			area.connect("mouse_exited", Callable(self, "_on_area_hover").bind(area, false))

	update_labels()

	show_message("🔍 Проверка наличия синглтона YandexInterstitial...", 2.0)
	if Engine.has_singleton("YandexInterstitial"):
		show_message("✅ Синглтон YandexInterstitial НАЙДЕН", 2.0)
		var ads = Engine.get_singleton("YandexInterstitial")
		show_message("🔌 Подключение сигнала onDebugMessage...", 2.0)
		if not ads.is_connected("onDebugMessage", Callable(self, "_on_yandex_debug")):
			ads.connect("onDebugMessage", Callable(self, "_on_yandex_debug"))
			show_message("✅ Сигнал onDebugMessage подключён", 2.0)
		else:
			show_message("⚠️ Сигнал onDebugMessage уже подключён", 2.0)
		show_message("🚀 Вызов ads.init()...", 2.0)
		ads.init()
	else:
		show_message("❌ Синглтон YandexInterstitial НЕ НАЙДЕН!", 3.0)
		show_message("❗ Реклама недоступна. Причина: плагин не загружен.", 3.0)
		show_message("🔧 Проверьте: 1) Custom Build, 2) Plugins в экспорте, 3) package name", 4.0)

func _process(_delta):
	if not Globals:
		return
	if Globals.Food >= 100:
		Globals.Survivors += 1
		Globals.Food -= 100
	update_labels()

func _on_yandex_debug(msg: String):
	print("YANDEX DEBUG:", msg)
	if msg.find("✅ Yandex SDK initialized") != -1:
		_is_sdk_ready = true
		show_message("✅ SDK готов к работе", 1.0)

func _on_Raid_area_input_event(_viewport, event, _shape_idx):
	if event is InputEventMouseButton and event.pressed:
		if Globals.Survivors <= 0:
			show_message("Нет выживших! Невозможно отправиться в рейд.")
			return
		if Globals.Food < 5 or Globals.Fuel < 3:
			show_message("Недостаточно еды или топлива для вылазки!")
			return

		if Engine.has_singleton("YandexInterstitial"):
			if not _is_sdk_ready:
				show_message("⏳ SDK ещё не готов. Подождите...", 2.0)
				return
			if _is_ad_in_progress:
				show_message("⏳ Реклама уже идёт...")
				return

			_is_ad_in_progress = true
			var ads = Engine.get_singleton("YandexInterstitial")

			if ads.is_connected("onInterstitialLoaded", Callable(self, "_on_interstitial_loaded")):
				ads.disconnect("onInterstitialLoaded", Callable(self, "_on_interstitial_loaded"))
			if ads.is_connected("onInterstitialError", Callable(self, "_on_interstitial_error")):
				ads.disconnect("onInterstitialError", Callable(self, "_on_interstitial_error"))
			if ads.is_connected("onInterstitialClosed", Callable(self, "_on_interstitial_closed")):
				ads.disconnect("onInterstitialClosed", Callable(self, "_on_interstitial_closed"))

			ads.connect("onInterstitialLoaded", Callable(self, "_on_interstitial_loaded"), CONNECT_ONE_SHOT | CONNECT_DEFERRED)
			ads.connect("onInterstitialError", Callable(self, "_on_interstitial_error"), CONNECT_ONE_SHOT | CONNECT_DEFERRED)
			ads.connect("onInterstitialClosed", Callable(self, "_on_interstitial_closed"), CONNECT_ONE_SHOT | CONNECT_DEFERRED)

			show_message("⏳ Загрузка рекламы перед походом...", 3.0)
			ads.loadInterstitial(YANDEX_INTERSTITIAL_ID)
			return
		else:
			if OS.has_feature("editor"):
				_proceed_to_raid()
			else:
				show_message("❗ Рейд недоступен без рекламы.", 3.0)

func _on_interstitial_loaded():
	show_message("✅ Реклама загружена! Показываем...", 1.5)
	await get_tree().create_timer(1.5).timeout
	if Engine.has_singleton("YandexInterstitial"):
		var ads = Engine.get_singleton("YandexInterstitial")
		ads.showInterstitial()

func _on_interstitial_error(error: String = "Не удалось загрузить рекламу"):
	_is_ad_in_progress = false
	show_message("❌ Ошибка рекламы: " + error, 3.0)
	if OS.has_feature("editor"):
		_proceed_to_raid()

func _on_interstitial_closed():
	_is_ad_in_progress = false
	_proceed_to_raid()

func _proceed_to_raid():
	Globals.add_food(-5)
	Globals.add_fuel(-3)
	show_message("➡️ Переход в рейд...", 1.0)
	await get_tree().create_timer(1.0).timeout
	get_tree().change_scene_to_file("res://Raid.tscn")

func update_labels():
	if not Globals:
		show_message("⚠️ Globals не найден!", 2.0)
		return
	if food_label: food_label.text = "Еда: " + str(Globals.Food)
	if meds_label: meds_label.text = "Медицина: " + str(Globals.Meds)
	if ammo_label: ammo_label.text = "Боеприпасы: " + str(Globals.Ammo)
	if metal_label: metal_label.text = "Металл: " + str(Globals.Metal)
	if fuel_label: fuel_label.text = "Топливо: " + str(Globals.Fuel)
	if water_label: water_label.text = "Вода: " + str(Globals.Water)
	if base_hp_label: base_hp_label.text = "Прочность базы: " + str(Globals.BaseHP)
	if survivors_label: survivors_label.text = "Выжившие: " + str(Globals.Survivors)

func _on_area_hover(area: Area2D, entered: bool):
	var sprite = area.get_parent()
	if sprite and sprite is Sprite2D:
		if entered:
			sprite.modulate = Color(1.3, 1.3, 1.3)
			sfx_hover.stream = hover_sound
			sfx_hover.play()
		else:
			sprite.modulate = Color(1, 1, 1)

func _click_flash(sprite: Sprite2D):
	if not sprite: return
	sprite.scale = Vector2(1.05, 1.05)
	sprite.modulate = Color(1.4, 1.4, 1.4)
	sfx_click.stream = click_sound
	sfx_click.play()
	await get_tree().create_timer(0.1).timeout
	sprite.scale = Vector2(1, 1)
	sprite.modulate = Color(1, 1, 1)

func _on_Medical_area_input_event(_viewport, event: InputEvent, _shape_idx):
	if event is InputEventMouseButton and event.pressed and event.button_index == MOUSE_BUTTON_LEFT:
		Globals.add_meds(1)
		_click_flash($Medical)

func _on_Fuel_area_input_event(_viewport, event: InputEvent, _shape_idx):
	if event is InputEventMouseButton and event.pressed and event.button_index == MOUSE_BUTTON_LEFT:
		Globals.add_fuel(1)
		_click_flash($Fuel)

func _on_Metal_area_input_event(_viewport, event: InputEvent, _shape_idx):
	if event is InputEventMouseButton and event.pressed and event.button_index == MOUSE_BUTTON_LEFT:
		Globals.add_metal(1)
		_click_flash($Metal)

func _on_Garden_area_input_event(_viewport, event: InputEvent, _shape_idx):
	if event is InputEventMouseButton and event.pressed and event.button_index == MOUSE_BUTTON_LEFT:
		if Globals.Water <= 0:
			show_message("Недостаточно воды для выращивания еды!")
			return
		Globals.add_water(-1)
		Globals.add_food(1)
		_click_flash($Garden)

func _on_Water_area_input_event(_viewport, event: InputEvent, _shape_idx):
	if event is InputEventMouseButton and event.pressed and event.button_index == MOUSE_BUTTON_LEFT:
		Globals.add_water(1)
		_click_flash($Water)

func _on_Weapon_area_input_event(_viewport, event: InputEvent, _shape_idx):
	if event is InputEventMouseButton and event.pressed and event.button_index == MOUSE_BUTTON_LEFT:
		if Globals.Metal <= 0:
			show_message("Недостаточно металла для производства патронов!")
			return
		Globals.add_metal(-1)
		Globals.add_ammo(1)
		_click_flash($Weapon)

func show_message(text: String, duration: float = MESSAGE_DURATION):
	if _message_timer:
		if _message_timer.timeout.is_connected(_on_base_message_timeout):
			_message_timer.timeout.disconnect(_on_base_message_timeout)
		_message_timer = null
	if _current_message:
		_current_message.queue_free()
		_current_message = null

	var canvas = get_tree().current_scene.get_node_or_null("CanvasLayer")
	if not canvas:
		return

	var panel = Panel.new()
	panel.name = "MessagePanel"
	panel.add_theme_color_override("panel", Color(0, 0, 0, 0.75))
	panel.anchor_left = 0.0
	panel.anchor_right = 1.0
	panel.anchor_bottom = 1.0
	panel.offset_top = -50
	panel.z_index = 100
	canvas.add_child(panel)

	var lbl = Label.new()
	lbl.text = text
	lbl.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	lbl.vertical_alignment = VERTICAL_ALIGNMENT_CENTER
	lbl.add_theme_font_size_override("font_size", 36)
	panel.add_child(lbl)

	lbl.anchor_left = 0.0
	lbl.anchor_right = 1.0
	lbl.anchor_top = 0.0
	lbl.anchor_bottom = 1.0
	lbl.offset_left = 10
	lbl.offset_right = -10

	_current_message = panel
	_message_timer = get_tree().create_timer(duration)
	_message_timer.timeout.connect(_on_base_message_timeout)

func _on_base_message_timeout():
	if _current_message and _current_message.get_parent():
		_current_message.queue_free()
		_current_message = null
	_message_timer = null
