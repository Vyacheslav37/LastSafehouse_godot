extends Node2D

const MESSAGE_DURATION := 2.0

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

func _ready():
	sfx_hover = AudioStreamPlayer.new()
	add_child(sfx_hover)
	sfx_click = AudioStreamPlayer.new()
	add_child(sfx_click)

	var dummy_player = AudioStreamPlayer.new()
	dummy_player.stream = preload("res://sounds/hover_click.ogg")
	dummy_player.name = "AudioDummy"
	add_child(dummy_player)

	for area in get_tree().get_nodes_in_group("interactables"):
		if not area.is_connected("mouse_entered", Callable(self, "_on_area_hover")):
			area.connect("mouse_entered", Callable(self, "_on_area_hover").bind(area, true))
		if not area.is_connected("mouse_exited", Callable(self, "_on_area_hover")):
			area.connect("mouse_exited", Callable(self, "_on_area_hover").bind(area, false))
	
	update_labels()
	# _add_background_to_labels()  # ← отключено, подложка уже в сценеels()

func _process(_delta):
	if Globals and Globals.Food >= 100:
		Globals.Survivors += 1
		Globals.Food -= 100
	update_labels()

func update_labels():
	if not Globals:
		push_warning("Globals autoload отсутствует.")
		return

	if food_label: food_label.text = "Еда: " + str(Globals.Food)
	if meds_label: meds_label.text = "Медицина: " + str(Globals.Meds)
	if ammo_label: ammo_label.text = "Боеприпасы: " + str(Globals.Ammo)
	if metal_label: metal_label.text = "Металл: " + str(Globals.Metal)
	if fuel_label: fuel_label.text = "Топливо: " + str(Globals.Fuel)
	if water_label: water_label.text = "Вода: " + str(Globals.Water)
	if base_hp_label: base_hp_label.text = "Прочность базы: " + str(Globals.BaseHP)
	if survivors_label: survivors_label.text = "Выжившие: " + str(Globals.Survivors)

func _add_background_to_labels():
	var labels = [food_label, meds_label, ammo_label, metal_label, fuel_label, water_label, base_hp_label, survivors_label]
	for label in labels:
		if not label or label.get_child_count() > 0:
			continue
		var panel = Panel.new()
		panel.name = "LabelBackground"
		panel.add_theme_color_override("panel", Color(0, 0, 0, 0.15))
		label.add_child(panel)
		panel.anchor_left = 0.0
		panel.anchor_top = 0.0
		panel.anchor_right = 1.0
		panel.anchor_bottom = 1.0
		panel.offset_left = -6
		panel.offset_top = -3
		panel.offset_right = -6
		panel.offset_bottom = -3
		panel.z_index = -1

func _on_area_hover(area: Area2D, entered: bool):
	var sprite = area.get_parent()
	if sprite and sprite is Sprite2D:
		if entered:
			sprite.modulate = Color(1.3, 1.3, 1.3)
			if hover_sound:
				sfx_hover.stream = hover_sound
				sfx_hover.play()
		else:
			sprite.modulate = Color(1, 1, 1)

func _click_flash(sprite: Sprite2D):
	if not sprite: return
	sprite.scale = Vector2(1.05, 1.05)
	sprite.modulate = Color(1.4, 1.4, 1.4)
	if click_sound:
		sfx_click.stream = click_sound
		sfx_click.play()
	await get_tree().create_timer(0.1).timeout
	sprite.scale = Vector2(1, 1)
	sprite.modulate = Color(1, 1, 1)

# === КЛИКИ ===

func _on_Raid_area_input_event(_viewport, event, _shape_idx):
	if event is InputEventMouseButton and event.pressed:
		if Globals.Survivors <= 0:
			show_message("Нет выживших! Невозможно отправиться в рейд.")
			return
		if Globals.Food < 5 or Globals.Fuel < 3:
			show_message("Недостаточно еды или топлива для вылазки!")
			return
		Globals.add_food(-5)
		Globals.add_fuel(-3)
		get_tree().change_scene_to_file("res://Raid.tscn")

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
		Globals.add_food(1)
		_click_flash($Garden)

func _on_Water_area_input_event(_viewport, event: InputEvent, _shape_idx):
	if event is InputEventMouseButton and event.pressed and event.button_index == MOUSE_BUTTON_LEFT:
		Globals.add_water(1)
		_click_flash($Water)

# 🔫 Боеприпасы: тратим металл, получаем патроны
func _on_Weapon_area_input_event(_viewport, event: InputEvent, _shape_idx):
	if event is InputEventMouseButton and event.pressed and event.button_index == MOUSE_BUTTON_LEFT:
		if Globals.Metal <= 0:
			show_message("Недостаточно металла для производства патронов!")
			return
		Globals.add_metal(-1)
		Globals.add_ammo(1)
		_click_flash($Weapon)

# === СИСТЕМА СООБЩЕНИЙ (как в рейде) ===

func show_message(text: String, duration: float = MESSAGE_DURATION):
	if _message_timer:
		_message_timer.timeout.disconnect(_on_base_message_timeout)
		_message_timer = null
	if _current_message:
		_current_message.queue_free()
		_current_message = null

	var panel = Panel.new()
	panel.name = "MessagePanel"
	panel.add_theme_color_override("panel", Color(0, 0, 0, 0.75))
	panel.anchor_left = 0.0
	panel.anchor_right = 1.0
	panel.anchor_bottom = 1.0
	panel.offset_left = 0
	panel.offset_right = 0
	panel.offset_bottom = 0
	panel.offset_top = -50
	panel.z_index = 100
	$CanvasLayer.add_child(panel)

	var lbl = Label.new()
	lbl.text = text
	lbl.autowrap_mode = TextServer.AUTOWRAP_OFF
	lbl.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	lbl.vertical_alignment = VERTICAL_ALIGNMENT_CENTER
	lbl.clip_text = true
	lbl.add_theme_font_size_override("font_size", 36)
	lbl.modulate = Color(1, 1, 1)
	panel.add_child(lbl)

	lbl.anchor_left = 0.0
	lbl.anchor_right = 1.0
	lbl.anchor_top = 0.0
	lbl.anchor_bottom = 1.0
	lbl.offset_left = 10
	lbl.offset_right = -10
	lbl.offset_top = 0
	lbl.offset_bottom = 0

	_current_message = panel
	_message_timer = get_tree().create_timer(duration)
	_message_timer.timeout.connect(_on_base_message_timeout)

func _on_base_message_timeout():
	if _current_message and _current_message.get_parent():
		_current_message.queue_free()
		_current_message = null
	_message_timer = null
