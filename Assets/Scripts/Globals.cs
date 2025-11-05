// Globals.cs
using UnityEngine;

public static class Globals
{
    public static int Food { get; private set; } = 10;
    public static int Meds { get; private set; } = 5;
    public static int Ammo { get; private set; } = 3;
    public static int Metal { get; private set; } = 8;
    public static int Fuel { get; private set; } = 6;
    public static int Water { get; private set; } = 7;
    public static int BaseHP { get; private set; } = 100;
    public static int Survivors { get; private set; } = 3;

    public static void AddFood(int amount) => Food = Mathf.Max(0, Food + amount);
    public static void AddMeds(int amount) => Meds = Mathf.Max(0, Meds + amount);
    public static void AddAmmo(int amount) => Ammo = Mathf.Max(0, Ammo + amount);
    public static void AddMetal(int amount) => Metal = Mathf.Max(0, Metal + amount);
    public static void AddFuel(int amount) => Fuel = Mathf.Max(0, Fuel + amount);
    public static void AddWater(int amount) => Water = Mathf.Max(0, Water + amount);
    public static void AddSurvivors(int amount) => Survivors = Mathf.Max(0, Survivors + amount);

    // ДОБАВЛЕНО: ДЛЯ РЕЙДА
    public static void AddBaseHP(int amount) => BaseHP = Mathf.Max(0, BaseHP + amount);
    public static void SetAmmo(int value) => Ammo = Mathf.Max(0, value);

    // ДОБАВЛЕНО: СБРОС В НАЧАЛЬНЫЕ ЗНАЧЕНИЯ
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
    }
}