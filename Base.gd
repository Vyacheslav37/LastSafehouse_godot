extends Node2D

# ⏱️ Длительность всплывающих сообщений (в секундах)
const MESSAGE_DURATION := 2.0

# Ссылки на UI-элементы
@onready var food_label = get_node_or_null("CanvasLayer/HBoxContainer/VBoxContainer/FoodLabel")
@onready var meds_label = get_node_or_null("CanvasLayer/HBoxContainer/VBoxContainer/MedsLabel")
@onready var ammo_label = get_node_or_null("CanvasLayer/HBoxContainer/VBoxContainer/AmmoLabel")
@onready var metal_label = get_node_or_null("CanvasLayer/HBoxContainer/VBoxContainer/MetalLabel")
@onready var fuel_label = get_node_or_null("CanvasLayer/HBoxContainer/VBoxContainer/FuelLabel")
@onready var water_label = get_node_or_null("CanvasLayer/HBoxContainer/VBoxContainer/WaterLabel")
@onready var base_hp_label = get_node_or_null("CanvasLayer/HBoxContainer/VBoxContainer/BaseHPLabel")
@onready var survivors_label = get_node_or_null("CanvasLayer/HBoxContainer/VBoxContainer/SurvivorsLabel")

# Звуки
@onready var hover_sound = preload("res://sounds/hover_click.ogg")
@onready var click_sound = preload("res://sounds/item_click.ogg")

var sfx_hover: AudioStreamPlayer
var sfx_click: AudioStreamPlayer

# Для управления динамическими сообщениями
var _current_message: Label = null
var _message_timer: SceneTreeTimer = null

func _ready():
	# Создаём плееры для звуков
	sfx_hover = AudioStreamPlayer.new()
	add_child(sfx_hover)
	sfx_click = AudioStreamPlayer.new()
	add_child(sfx_click)

	# 🔊 Dummy-плеер для корректного экспорта аудио в HTML5
	var dummy_player = AudioStreamPlayer.new()
	dummy_player.stream = preload("res://sounds/hover_click.ogg")
	dummy_player.name = "AudioDummy"
	add_child(dummy_player)

	# Подключаем hover-подсветку ко всем интерактивным зонам
	for area in get_tree().get_nodes_in_group("interactables"):
		if not area.is_connected("mouse_entered", Callable(self, "_on_area_hover")):
			area.connect("mouse_entered", Callable(self, "_on_area_hover").bind(area, true))
		if not area.is_connected("mouse_exited", Callable(self, "_on_area_hover")):
			area.connect("mouse_exited", Callable(self, "_on_area_hover").bind(area, false))
	
	print("Игра запущена. Проверяем узлы...")
	update_labels()

func _process(_delta):
	update_labels()

func update_labels():
	if not Globals:
		push_warning("Globals autoload отсутствует. Добавь Globals.gd в Project → Autoload как 'Globals'.")
		return

	# Обновляем текст на экране
	if food_label: food_label.text = "Еда: " + str(Globals.Food)
	if meds_label: meds_label.text = "Медицина: " + str(Globals.Meds)
	if ammo_label: ammo_label.text = "Боеприпасы: " + str(Globals.Ammo)
	if metal_label: metal_label.text = "Металл: " + str(Globals.Metal)
	if fuel_label: fuel_label.text = "Топливо: " + str(Globals.Fuel)
	if water_label: water_label.text = "Вода: " + str(Globals.Water)
	if base_hp_label: base_hp_label.text = "Прочность базы: " + str(Globals.BaseHP)
	if survivors_label: survivors_label.text = "Выжившие: " + str(Globals.Survivors)

# Hover-подсветка
func _on_area_hover(area: Area2D, entered: bool):
	var sprite = area.get_parent()
	if sprite and sprite is Sprite2D:
		if entered:
			sprite.modulate = Color(1.3, 1.3, 1.3)
			if hover_sound:
				sfx_hover.stream = hover_sound
				sfx_hover.play()
				print("Звук ховера для ", area.name)
		else:
			sprite.modulate = Color(1, 1, 1)

# Визуальный отклик клика
func _click_flash(sprite: Sprite2D):
	if not sprite: return
	sprite.scale = Vector2(1.05, 1.05)
	sprite.modulate = Color(1.4, 1.4, 1.4)
	if click_sound:
		sfx_click.stream = click_sound
		sfx_click.play()
		print("Звук клика для ", sprite.name)
	await get_tree().create_timer(0.1).timeout
	sprite.scale = Vector2(1, 1)
	sprite.modulate = Color(1, 1, 1)

# Обработчики кликов
func _on_Raid_area_input_event(_viewport, event, _shape_idx):
	if event is InputEventMouseButton and event.pressed:
		print("Попытка рейда...")
		if Globals.Food < 5 or Globals.Fuel < 3:
			show_message("Недостаточно еды или топлива для вылазки!")
			return
		Globals.add_food(-5)
		Globals.add_fuel(-3)
		get_tree().change_scene_to_file("res://Raid.tscn")
		print("Переход в Raid.tscn")

func _on_Medical_area_input_event(_viewport, event: InputEvent, _shape_idx):
	if event is InputEventMouseButton and event.pressed and event.button_index == MOUSE_BUTTON_LEFT:
		print("Клик по MedicalArea")
		Globals.add_meds(1)
		_click_flash($Medical)

func _on_Fuel_area_input_event(_viewport, event: InputEvent, _shape_idx):
	if event is InputEventMouseButton and event.pressed and event.button_index == MOUSE_BUTTON_LEFT:
		print("Клик по FuelArea")
		Globals.add_fuel(1)
		_click_flash($Fuel)

func _on_Metal_area_input_event(_viewport, event: InputEvent, _shape_idx):
	if event is InputEventMouseButton and event.pressed and event.button_index == MOUSE_BUTTON_LEFT:
		print("Клик по MetalArea")
		Globals.add_metal(1)
		_click_flash($Metal)

func _on_Garden_area_input_event(_viewport, event: InputEvent, _shape_idx):
	if event is InputEventMouseButton and event.pressed and event.button_index == MOUSE_BUTTON_LEFT:
		print("Клик по GardenArea")
		Globals.add_food(1)
		_click_flash($Garden)

func _on_Water_area_input_event(_viewport, event: InputEvent, _shape_idx):
	if event is InputEventMouseButton and event.pressed and event.button_index == MOUSE_BUTTON_LEFT:
		print("Клик по WaterArea")
		Globals.add_water(1)
		_click_flash($Water)

func _on_Weapon_area_input_event(_viewport, event: InputEvent, _shape_idx):
	if event is InputEventMouseButton and event.pressed and event.button_index == MOUSE_BUTTON_LEFT:
		print("Клик по WeaponArea")
		Globals.add_ammo(1)
		_click_flash($Weapon)

# Показывает всплывающее сообщение по центру экрана (как в Raid)
func show_message(text: String, duration: float = MESSAGE_DURATION):
	# Отменяем предыдущий таймер
	if _message_timer:
		_message_timer.timeout.disconnect(_on_base_message_timeout)
		_message_timer = null

	# Удаляем старое сообщение
	if _current_message:
		_current_message.queue_free()
		_current_message = null

	# Создаём новое
	var lbl = Label.new()
	lbl.text = text
	lbl.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	lbl.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	lbl.vertical_alignment = VERTICAL_ALIGNMENT_CENTER
	lbl.add_theme_font_size_override("font_size", 48)

	# Центрирование
	lbl.anchor_left = 0.5
	lbl.anchor_top = 0.5
	lbl.anchor_right = 0.5
	lbl.anchor_bottom = 0.5
	lbl.offset_left = -400
	lbl.offset_right = 400
	lbl.offset_top = -100
	lbl.offset_bottom = 100

	lbl.modulate = Color(1, 1, 1)  # белый, непрозрачный
	$CanvasLayer.add_child(lbl)
	_current_message = lbl

	# Запускаем таймер
	_message_timer = get_tree().create_timer(duration)
	_message_timer.timeout.connect(_on_base_message_timeout)

func _on_base_message_timeout():
	if _current_message:
		_current_message.queue_free()
		_current_message = null
	_message_timer = null
