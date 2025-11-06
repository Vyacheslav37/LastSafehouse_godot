// Globals.cs
using UnityEngine;

public static class Globals
{
    // --- Ключи для PlayerPrefs ---
    private const string KEY_FOOD = "Food";
    private const string KEY_MEDS = "Meds";
    private const string KEY_AMMO = "Ammo";
    private const string KEY_METAL = "Metal";
    private const string KEY_FUEL = "Fuel";
    private const string KEY_WATER = "Water";
    private const string KEY_BASE_HP = "BaseHP";
    private const string KEY_SURVIVORS = "Survivors";

    // --- Свойства ---
    public static int Food { get; private set; } = 10;
    public static int Meds { get; private set; } = 5;
    public static int Ammo { get; private set; } = 3;
    public static int Metal { get; private set; } = 8;
    public static int Fuel { get; private set; } = 6;
    public static int Water { get; private set; } = 7;
    public static int BaseHP { get; private set; } = 100;
    public static int Survivors { get; private set; } = 3;

    // --- Статический конструктор: загружает прогресс при первом обращении ---
    static Globals()
    {
        Load();
    }

    // --- Методы изменения ресурсов ---
    public static void AddFood(int amount) => Food = Mathf.Max(0, Food + amount);
    public static void AddMeds(int amount) => Meds = Mathf.Max(0, Meds + amount);
    public static void AddAmmo(int amount) => Ammo = Mathf.Max(0, Ammo + amount);
    public static void AddMetal(int amount) => Metal = Mathf.Max(0, Metal + amount);
    public static void AddFuel(int amount) => Fuel = Mathf.Max(0, Fuel + amount);
    public static void AddWater(int amount) => Water = Mathf.Max(0, Water + amount);
    public static void AddSurvivors(int amount) => Survivors = Mathf.Max(0, Survivors + amount);
    public static void AddBaseHP(int amount) => BaseHP = Mathf.Max(0, BaseHP + amount);
    public static void SetAmmo(int value) => Ammo = Mathf.Max(0, value);

    // --- Сохранение прогресса ---
    public static void Save()
    {
        PlayerPrefs.SetInt(KEY_FOOD, Food);
        PlayerPrefs.SetInt(KEY_MEDS, Meds);
        PlayerPrefs.SetInt(KEY_AMMO, Ammo);
        PlayerPrefs.SetInt(KEY_METAL, Metal);
        PlayerPrefs.SetInt(KEY_FUEL, Fuel);
        PlayerPrefs.SetInt(KEY_WATER, Water);
        PlayerPrefs.SetInt(KEY_BASE_HP, BaseHP);
        PlayerPrefs.SetInt(KEY_SURVIVORS, Survivors);
        PlayerPrefs.Save();
    }

    // --- Загрузка прогресса ---
    public static void Load()
    {
        Food = PlayerPrefs.GetInt(KEY_FOOD, 10);
        Meds = PlayerPrefs.GetInt(KEY_MEDS, 5);
        Ammo = PlayerPrefs.GetInt(KEY_AMMO, 3);
        Metal = PlayerPrefs.GetInt(KEY_METAL, 8);
        Fuel = PlayerPrefs.GetInt(KEY_FUEL, 6);
        Water = PlayerPrefs.GetInt(KEY_WATER, 7);
        BaseHP = PlayerPrefs.GetInt(KEY_BASE_HP, 100);
        Survivors = PlayerPrefs.GetInt(KEY_SURVIVORS, 3);
    }

    // --- Сброс прогресса до начальных значений ---
    public static void ResetAll()
    {
        Food = 10;
        Meds = 5;
        Ammo = 3;
        Metal = 8;
        Fuel = 6;
        Water = 7;
        BaseHP = 100;
        Survivors = 3;
        Save(); // Сохраняем сброс на устройство
    }
}