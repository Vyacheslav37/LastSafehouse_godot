extends Node

# === Глобальные переменные ресурсов ===
var Food: int = 0
var Meds: int = 0
var Ammo: int = 20
var Metal: int = 0
var Fuel: int = 0
var Water: int = 0
var BaseHP: int = 100
var Survivors: int = 1

const SAVE_PATH := "user://savegame.json"

func _ready():
	load_globals()

# === Форматированный словарь для UI ===
func format_resources() -> Dictionary:
	return {
		"Food": str(Food),
		"Meds": str(Meds),
		"Ammo": str(Ammo),
		"Metal": str(Metal),
		"Fuel": str(Fuel),
		"Water": str(Water),
		"BaseHP": str(BaseHP),
		"Survivors": str(Survivors)
	}

# === Универсальные функции изменения значений с автосейвом ===
func add_food(v: int):
	Food = max(0, Food + v)
	save_globals()

func add_meds(v: int):
	Meds = max(0, Meds + v)
	save_globals()

func add_ammo(v: int):
	Ammo = max(0, Ammo + v)
	save_globals()

func add_metal(v: int):
	Metal = max(0, Metal + v)
	save_globals()

func add_fuel(v: int):
	Fuel = max(0, Fuel + v)
	save_globals()

func add_water(v: int):
	Water = max(0, Water + v)
	save_globals()

func change_base_hp(v: int):
	BaseHP = max(0, BaseHP + v)
	save_globals()

func change_survivors(v: int):
	Survivors = max(0, Survivors + v)
	save_globals()

# === Сохранение ===
func save_globals():
	var d = {
		"Food": Food,
		"Meds": Meds,
		"Ammo": Ammo,
		"Metal": Metal,
		"Fuel": Fuel,
		"Water": Water,
		"BaseHP": BaseHP,
		"Survivors": Survivors
	}
	var file = FileAccess.open(SAVE_PATH, FileAccess.WRITE)
	if file:
		file.store_string(JSON.stringify(d))
		file.close()
	else:
		push_error("Не удалось открыть файл для записи: %s" % SAVE_PATH)

# === Загрузка ===
func load_globals():
	if not FileAccess.file_exists(SAVE_PATH):
		save_globals()
		return

	var file = FileAccess.open(SAVE_PATH, FileAccess.READ)
	if not file:
		push_error("Не удалось открыть файл для чтения: %s" % SAVE_PATH)
		return

	var raw = file.get_as_text()
	file.close()

	var d = JSON.parse_string(raw)
	if typeof(d) == TYPE_DICTIONARY:
		Food = int(d.get("Food", Food))
		Meds = int(d.get("Meds", Meds))
		Ammo = int(d.get("Ammo", Ammo))
		Metal = int(d.get("Metal", Metal))
		Fuel = int(d.get("Fuel", Fuel))
		Water = int(d.get("Water", Water))
		BaseHP = int(d.get("BaseHP", BaseHP))
		Survivors = int(d.get("Survivors", Survivors))
	else:
		push_warning("Ошибка при загрузке JSON — создаю новый save.")
		save_globals()
