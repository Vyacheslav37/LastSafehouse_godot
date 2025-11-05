using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

public class BaseManager : MonoBehaviour
{
    public enum ItemType { Garden, Fuel, Medical, Metal, Water, Weapon, Raid }
    private const float MESSAGE_DURATION = 2.0f;

    [Header("UI Labels — СПИСОК СЛЕВА")]
    public TextMeshProUGUI foodLabel;
    public TextMeshProUGUI medsLabel;
    public TextMeshProUGUI ammoLabel;
    public TextMeshProUGUI metalLabel;
    public TextMeshProUGUI fuelLabel;
    public TextMeshProUGUI waterLabel;
    public TextMeshProUGUI baseHpLabel;
    public TextMeshProUGUI survivorsLabel;

    [Header("Message System")]
    public GameObject messagePanel;
    public TextMeshProUGUI messageText;

    [Header("Sounds")]
    public AudioClip clickSound;
    private AudioSource sfxClick;
    private Coroutine messageCoroutine;

    public static BaseManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        sfxClick = gameObject.AddComponent<AudioSource>();
        UpdateLabels();
        if (messagePanel) messagePanel.SetActive(false);

        if (PlayerPrefs.HasKey("RaidResult"))
        {
            string result = PlayerPrefs.GetString("RaidResult");
            ShowMessage(result, 4f);
            PlayerPrefs.DeleteKey("RaidResult");
        }
    }

    private void Update()
    {
        if (Globals.Food >= 100)
        {
            Globals.AddSurvivors(1);
            Globals.AddFood(-100);
            UpdateLabels();
        }
    }

    public void OnItemClicked(Interactable item)
    {
        if (!item || !item.Sprite) return;

        StartCoroutine(ClickFlash(item.Sprite));
        if (clickSound) sfxClick.PlayOneShot(clickSound);

        switch (item.itemType)
        {
            case ItemType.Raid:
                if (Globals.Survivors <= 0)
                {
                    ShowMessage("Нет выживших для рейда!");
                    return;
                }
                if (Globals.Food < 5 || Globals.Fuel < 3)
                {
                    ShowMessage("Нужно 5 еды и 3 топлива!");
                    return;
                }
                Globals.AddFood(-5);
                Globals.AddFuel(-3);
                SceneManager.LoadScene("Raid");
                break;

            case ItemType.Garden:
                if (Globals.Water <= 0)
                {
                    ShowMessage("Нет воды для сада!");
                    return;
                }
                Globals.AddWater(-1);
                Globals.AddFood(1);
                break;

            case ItemType.Fuel: Globals.AddFuel(1); break;
            case ItemType.Medical: Globals.AddMeds(1); break;
            case ItemType.Metal: Globals.AddMetal(1); break;
            case ItemType.Water: Globals.AddWater(1); break;

            case ItemType.Weapon:
                if (Globals.Metal <= 0)
                {
                    ShowMessage("Нет металла для оружия!");
                    return;
                }
                Globals.AddMetal(-1);
                Globals.AddAmmo(1);
                break;
        }

        UpdateLabels();
    }

    private IEnumerator ClickFlash(SpriteRenderer sprite)
    {
        if (!sprite) yield break;

        Vector3 original = sprite.transform.localScale;
        sprite.transform.localScale = original * 1.1f;
        sprite.color = new Color(1.5f, 1.5f, 1.5f);

        yield return new WaitForSecondsRealtime(0.08f);

        sprite.transform.localScale = original;
        sprite.color = Color.white;
    }

    public void ShowMessage(string text, float duration = MESSAGE_DURATION)
    {
        if (messageCoroutine != null) StopCoroutine(messageCoroutine);
        messageCoroutine = StartCoroutine(ShowMessageRoutine(text, duration));
    }

    private IEnumerator ShowMessageRoutine(string text, float duration)
    {
        if (messagePanel) messagePanel.SetActive(true);
        if (messageText) messageText.text = text;
        yield return new WaitForSeconds(duration);
        if (messagePanel) messagePanel.SetActive(false);
        messageCoroutine = null;
    }

    private void UpdateLabels()
    {
        if (foodLabel) foodLabel.text = $"Еда: {Globals.Food}";
        if (medsLabel) medsLabel.text = $"Медицина: {Globals.Meds}";
        if (ammoLabel) ammoLabel.text = $"Боеприпасы: {Globals.Ammo}";
        if (metalLabel) metalLabel.text = $"Металл: {Globals.Metal}";
        if (fuelLabel) fuelLabel.text = $"Топливо: {Globals.Fuel}";
        if (waterLabel) waterLabel.text = $"Вода: {Globals.Water}";
        if (baseHpLabel) baseHpLabel.text = $"База: {Globals.BaseHP}";
        if (survivorsLabel) survivorsLabel.text = $"Выжившие: {Globals.Survivors}";
    }
}