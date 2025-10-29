extends Node2D

const MESSAGE_DURATION := 3.0

@onready var zombie_label = $CanvasLayer/InfoBackground/ZombieLabel
@onready var ammo_label = $CanvasLayer/InfoBackground/AmmoLabel
@onready var survivors_label = $CanvasLayer/InfoBackground/SurvivorsLabel

# Звуки
@onready var hover_sound = preload("res://sounds/hover_click.ogg")
@onready var click_sound = preload("res://sounds/item_click.ogg")

var sfx_hover: AudioStreamPlayer
var sfx_click: AudioStreamPlayer

var zombies: int = 0
var local_ammo: int = 0
var raid_survivors: int = 0
var base_ammo_before: int = 0
var started := false

var _current_message: Control = null
var _message_timer: SceneTreeTimer = null

func _ready():
	if not Globals:
		push_error("Globals не загружен! Добавь Globals.gd в Autoload.")
		return

	sfx_hover = AudioStreamPlayer.new()
	add_child(sfx_hover)
	sfx_click = AudioStreamPlayer.new()
	add_child(sfx_click)

	randomize()
	_start_raid()

	# Подключаем hover к зомби-зонам (эффекты отключены)
	var zombie_areas = [ $Zombie1/Zombie1Area, $Zombie2/Zombie2Area ]
	for area in zombie_areas:
		if area:
			if not area.is_connected("mouse_entered", Callable(self, "_on_zombie_hover")):
				area.connect("mouse_entered", Callable(self, "_on_zombie_hover").bind(area, true))
			if not area.is_connected("mouse_exited", Callable(self, "_on_zombie_hover")):
				area.connect("mouse_exited", Callable(self, "_on_zombie_hover").bind(area, false))

func _start_raid():
	raid_survivors = max(1, min(Globals.Survivors, randi_range(1, Globals.Survivors)))
	zombies = raid_survivors * randi_range(5, 15)

	var cost_food = raid_survivors * randi_range(1, 2)
	var cost_fuel = raid_survivors * randi_range(1, 2)

	Globals.Food = max(0, Globals.Food - cost_food)
	Globals.Fuel = max(0, Globals.Fuel - cost_fuel)
	base_ammo_before = Globals.Ammo
	local_ammo = min(Globals.Ammo, raid_survivors * 10)

	if Globals.has_method("save"):
		Globals.save()

	started = true
	update_ui()
	_show_temporary_message("Рейд: %d выживших | Еда -%d, Топливо -%d, Боеприпасы %d" % [raid_survivors, cost_food, cost_fuel, local_ammo])

func update_ui():
	if zombie_label: zombie_label.text = "Зомби: %d" % zombies
	if ammo_label: ammo_label.text = "Боеприпасы: %d" % local_ammo
	if survivors_label: survivors_label.text = "Выживших в рейде: %d" % raid_survivors

# === HOVER НА ЗОМБИ === (отключено)
func _on_zombie_hover(_area: Area2D, _entered: bool):
	pass

# === ВИЗУАЛЬНЫЙ ОТКЛИК КЛИКА ===
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

# === КЛИКИ ПО ЗОМБИ ===
func _on_zombie1_input(_viewport, event, _shape_idx):
	if not started:
		return
	if event is InputEventMouseButton and event.pressed and event.button_index == MOUSE_BUTTON_LEFT:
		_click_flash($Zombie1)
		_on_attack_by_click()

func _on_zombie2_input(_viewport, event, _shape_idx):
	if not started:
		return
	if event is InputEventMouseButton and event.pressed and event.button_index == MOUSE_BUTTON_LEFT:
		_click_flash($Zombie2)
		_on_attack_by_click()

# === ТАП ПО GO BASE → ОТСТУПЛЕНИЕ ===
func _on_go_base_input(_viewport, event, _shape_idx):
	if event is InputEventMouseButton and event.pressed and event.button_index == MOUSE_BUTTON_LEFT:
		_click_flash($GoBase)
		_retreat()

func _on_attack_by_click():
	if local_ammo <= 0:
		_show_temporary_message("Боеприпасов нет!")
		return

	local_ammo -= 1
	var killed = randi_range(1, min(3, zombies))
	zombies = max(0, zombies - killed)
	update_ui()

	if zombies <= 0:
		_on_victory()

# === ОТСТУПЛЕНИЕ: потери ≤10%, медицина только на раненых ===
func _retreat():
	var max_lost = max(1, int(round(raid_survivors * 0.1)))  # максимум 10% от рейда
	var dead = randi_range(0, max_lost)
	var wounded = randi_range(0, max_lost - dead)

	# Сколько раненых можно вылечить?
	var healed_wounded = min(wounded, Globals.Meds)
	# Остальные раненые погибают
	var final_dead = dead + (wounded - healed_wounded)
	var final_wounded = healed_wounded

	Globals.Survivors = max(0, Globals.Survivors - final_dead - final_wounded)
	Globals.Meds = max(0, Globals.Meds - healed_wounded)

	# Возвращаем неиспользованные боеприпасы
	Globals.Ammo = base_ammo_before - (min(base_ammo_before, raid_survivors * 10) - local_ammo)

	if Globals.has_method("save"):
		Globals.save()

	_show_temporary_message("Отступление! Погибло: %d, ранено: %d, медицины: -%d" % [final_dead, final_wounded, healed_wounded])
	_message_timer = get_tree().create_timer(MESSAGE_DURATION)
	_message_timer.timeout.connect(_on_retreat_timeout)

func _on_retreat_timeout():
	_return_to_base()

func _on_victory():
	var g_food = raid_survivors * randi_range(2, 4)
	var g_metal = raid_survivors * randi_range(1, 3)
	var g_ammo_reward = raid_survivors * randi_range(1, 2)

	Globals.Food += g_food
	Globals.Metal += g_metal
	Globals.Ammo = local_ammo + g_ammo_reward
	Globals.BaseHP += raid_survivors

	if Globals.has_method("save"):
		Globals.save()

	_show_temporary_message("Победа! Еда +%d, Металл +%d, Боеприпасы +%d, База +%d" % [g_food, g_metal, g_ammo_reward, raid_survivors])
	_message_timer = get_tree().create_timer(MESSAGE_DURATION)
	_message_timer.timeout.connect(_on_victory_timeout)

func _on_victory_timeout():
	_return_to_base()

func _return_to_base():
	get_tree().change_scene_to_file("res://Base.tscn")

# === СИСТЕМА СООБЩЕНИЙ ===
func _show_temporary_message(text: String, duration: float = MESSAGE_DURATION):
	if _message_timer:
		_message_timer.timeout.disconnect(_on_message_timeout)
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
	lbl.add_theme_font_size_override("font_size", 28)
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
	_message_timer.timeout.connect(_on_message_timeout)

func _on_message_timeout():
	if _current_message and _current_message.get_parent():
		_current_message.queue_free()
		_current_message = null
	_message_timer = null
