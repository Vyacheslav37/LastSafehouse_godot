# Raid.gd
extends Node2D

# ⏱️ Настройка длительности всплывающих сообщений (в секундах)
const MESSAGE_DURATION := 2.0

@onready var zombie_label = $CanvasLayer/ZombieLabel
@onready var ammo_label = $CanvasLayer/AmmoLabel
@onready var attack_btn = $CanvasLayer/AttackButton
@onready var retreat_btn = $CanvasLayer/RetreatButton

var zombies: int = 0
var local_ammo: int = 0
var cost_food := 0
var cost_fuel := 0
var cost_ammo := 0
var started := false

# Для управления динамическими сообщениями
var _current_message: Label = null
var _message_timer: SceneTreeTimer = null

func _ready():
	if not Globals:
		push_error("Globals не загружен! Добавь Globals.gd в Autoload.")
		return
	randomize()
	_start_raid()

func _start_raid():
	# генерируем зомби и списываем начальные ресурсы
	zombies = randi_range(10, 100)
	cost_food = randi_range(1, 3)
	cost_fuel = randi_range(1, 2)
	cost_ammo = randi_range(5, 10)

	# снимаем ресурсы (не уходим в отрицательное)
	Globals.Food = max(0, Globals.Food - cost_food)
	Globals.Fuel = max(0, Globals.Fuel - cost_fuel)
	Globals.Ammo = max(0, Globals.Ammo - cost_ammo)

	# сохраняем, если есть метод save()
	if Globals.has_method("save"):
		Globals.save()

	# локальный ammo — то, что осталось для рейда
	local_ammo = Globals.Ammo

	started = true
	update_ui()
	_show_temporary_message("Выход: -Еда %d, -Топливо %d, -Боеприпасы %d" % [cost_food, cost_fuel, cost_ammo])

func update_ui():
	if zombie_label: zombie_label.text = "Зомби: %d" % zombies
	if ammo_label: ammo_label.text = "Боеприпасы: %d" % local_ammo

# Универсальный клик по зомби (срабатывает для Zombie1 и Zombie2)
func _on_zombie_input(_viewport, event, _shape_idx):
	if not started:
		return
	if event is InputEventMouseButton and event.pressed and event.button_index == MOUSE_BUTTON_LEFT:
		_on_attack_by_click()

# Кнопка Атаковать (альтернатива клику по зомби)
func _on_attack_pressed():
	_on_attack_by_click()

func _on_attack_by_click():
	if local_ammo <= 0:
		_show_temporary_message("Патронов нет!")
		return

	# тратим 1 патрон
	local_ammo -= 1

	# убиваем рандомно 1..3 зомби
	var killed = randi_range(1, 3)
	zombies = max(0, zombies - killed)

	# синхронизируем с глобальными патронами
	Globals.Ammo = local_ammo
	if Globals.has_method("save"):
		Globals.save()

	update_ui()

	if zombies <= 0:
		_on_victory()

func _on_retreat_pressed():
	_retreat()

func _retreat():
	# Потери при отступлении: людей и медицина рандомно
	var lost_people = 0
	if Globals.Survivors > 0:
		lost_people = randi_range(1, min(Globals.Survivors, 2))
	Globals.Survivors = max(0, Globals.Survivors - lost_people)

	var lost_meds = 0
	if Globals.Meds > 0:
		lost_meds = randi_range(1, min(Globals.Meds, 3))
	Globals.Meds = max(0, Globals.Meds - lost_meds)

	# сохраняем патроны в глобальные
	Globals.Ammo = local_ammo
	if Globals.has_method("save"):
		Globals.save()

	_show_temporary_message("Отступили. Потери: выживших — %d, медикаментов — %d" % [lost_people, lost_meds])
	await get_tree().create_timer(MESSAGE_DURATION).timeout
	_return_to_base()

func _on_victory():
	# Награды
	var g_food = randi_range(2, 7)
	var g_metal = randi_range(1, 4)
	var g_ammo_reward = randi_range(1, 5)

	Globals.Food += g_food
	Globals.Metal += g_metal
	Globals.Ammo += g_ammo_reward

	if Globals.has_method("save"):
		Globals.save()

	_show_temporary_message("Победа! +Еда %d, +Металл %d, +Боеприпасы %d" % [g_food, g_metal, g_ammo_reward])
	await get_tree().create_timer(MESSAGE_DURATION).timeout
	_return_to_base()

func _return_to_base():
	get_tree().change_scene_to_file("res://Base.tscn")

# Показываем временное сообщение по центру экрана (без анимации, одно за раз)
func _show_temporary_message(text: String, duration: float = MESSAGE_DURATION):
	# Отменяем предыдущий таймер
	if _message_timer:
		_message_timer.timeout.disconnect(_on_message_timeout)
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

	# Центрирование через anchor
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

	# Запускаем новый таймер
	_message_timer = get_tree().create_timer(duration)
	_message_timer.timeout.connect(_on_message_timeout)

func _on_message_timeout():
	if _current_message:
		_current_message.queue_free()
		_current_message = null
	_message_timer = null
