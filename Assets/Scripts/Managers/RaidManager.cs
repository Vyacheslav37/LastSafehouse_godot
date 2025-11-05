// RaidManager.cs
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class RaidManager : MonoBehaviour
{
    public TMP_Text buttonText;

    void Start()
    {
        var button = GetComponent<Button>();
        if (button)
            button.onClick.AddListener(GoBack);
    }

    void GoBack()
    {
        // Награда
        int food = Random.Range(5, 15);
        int meds = Random.Range(0, 3);
        int ammo = Random.Range(2, 8);
        int metal = Random.Range(3, 10);
        int fuel = Random.Range(1, 5);

        Globals.AddFood(food);
        Globals.AddMeds(meds);
        Globals.AddAmmo(ammo);
        Globals.AddMetal(metal);
        Globals.AddFuel(fuel);

        // Шанс потери
        string result = $"Рейд завершён!\n" +
                        $"+Еда: {food}\n" +
                        $"+Медицина: {meds}\n" +
                        $"+Патроны: {ammo}\n" +
                        $"+Металл: {metal}\n" +
                        $"+Топливо: {fuel}";

        if (Random.value < 0.3f)
        {
            Globals.AddSurvivors(-1);
            result += "\n\nПотерян 1 выживший...";
        }

        PlayerPrefs.SetString("RaidResult", result);
        PlayerPrefs.Save();

        SceneManager.LoadScene("BaseScene");
    }
}