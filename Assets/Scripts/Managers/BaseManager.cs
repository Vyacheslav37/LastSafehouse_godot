using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

public class BaseManager : MonoBehaviour
{
    public enum ItemType { Garden, Fuel, Medical, Metal, Water, Weapon, Raid, Zombie, GoBase }

    private const float MESSAGE_DURATION = 2.0f;
    private const float CLICK_COOLDOWN = 0.1f;

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
    private float lastClickTime;

    public static BaseManager Instance { get; private set; }

    private void Awake()
    {
        Debug.Log($"[BaseManager] Awake: Instance = {(Instance != null ? "УЖЕ ЕСТЬ" : "СОЗДАЁТСЯ")}");
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[BaseManager] ДУБЛИКАТ! Уничтожаем.");
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        Debug.Log("[BaseManager] Синглтон создан. DontDestroyOnLoad.");

        SceneManager.sceneLoaded += OnSceneLoaded;
        Debug.Log("[BaseManager] Подписка на sceneLoaded.");
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        Debug.Log("[BaseManager] Отписка от sceneLoaded. Уничтожен.");
    }

    private void Start()
    {
        Debug.Log("[BaseManager] Start: Инициализация...");
        sfxClick = gameObject.AddComponent<AudioSource>();
        Debug.Log($"[BaseManager] AudioSource добавлен: {sfxClick != null}");

        UpdateLabels();
        if (messagePanel)
        {
            messagePanel.SetActive(false);
            Debug.Log("[BaseManager] messagePanel деактивирован.");
        }
        else
        {
            Debug.LogWarning("[BaseManager] messagePanel НЕ НАЗНАЧЕН!");
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"[BaseManager] OnSceneLoaded: {scene.name} | Mode: {mode}");
        if (scene.name == "BaseScene")
        {
            Debug.Log("[BaseManager] BaseScene загружена → UpdateLabels()");
            UpdateLabels();
        }
    }

    private void Update()
    {
        if (Globals.Food >= 100)
        {
            Debug.Log($"[BaseManager] Авто-выжившие: Еда >= 100 → +1 выживший, -100 еды");
            Globals.AddSurvivors(1);
            Globals.AddFood(-100);
            UpdateLabels();
        }
    }

    public void OnItemClicked(Interactable item)
    {
        Debug.Log($"[BaseManager] OnItemClicked: {item.name} | Type: {item.itemType}");

        if (Time.realtimeSinceStartup - lastClickTime < CLICK_COOLDOWN)
        {
            Debug.LogWarning("[BaseManager] Клик слишком быстро! Кулдаун.");
            return;
        }
        if (!item || !item.Sprite)
        {
            Debug.LogError("[BaseManager] item или Sprite = null!");
            return;
        }

        lastClickTime = Time.realtimeSinceStartup;

        if (SceneManager.GetActiveScene().name == "BaseScene")
        {
            Debug.Log("[BaseManager] Анимация клика + звук");
            StartCoroutine(ClickFlash(item.Sprite));
            if (clickSound) sfxClick.PlayOneShot(clickSound);
        }

        switch (item.itemType)
        {
            case ItemType.Raid:
                Debug.Log($"[BaseManager] Рейд: Выжившие={Globals.Survivors}, Еда={Globals.Food}, Топливо={Globals.Fuel}");
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
                Debug.Log("[BaseManager] Списано: -5 еды, -3 топлива → Загрузка Raid");
                SceneManager.LoadScene("Raid");
                break;

            case ItemType.Garden:
                if (Globals.Water <= 0) { ShowMessage("Нет воды для сада!"); return; }
                Globals.AddWater(-1); Globals.AddFood(1);
                Debug.Log("[BaseManager] Сад: -1 вода, +1 еда");
                break;

            case ItemType.Fuel: Globals.AddFuel(1); Debug.Log("[BaseManager] +1 топливо"); break;
            case ItemType.Medical: Globals.AddMeds(1); Debug.Log("[BaseManager] +1 медицина"); break;
            case ItemType.Metal: Globals.AddMetal(1); Debug.Log("[BaseManager] +1 металл"); break;
            case ItemType.Water: Globals.AddWater(1); Debug.Log("[BaseManager] +1 вода"); break;

            case ItemType.Weapon:
                if (Globals.Metal <= 0) { ShowMessage("Нет металла для оружия!"); return; }
                Globals.AddMetal(-1); Globals.AddAmmo(1);
                Debug.Log("[BaseManager] Оружие: -1 металл, +1 патроны");
                break;
        }

        UpdateLabels();
    }

    private IEnumerator ClickFlash(SpriteRenderer sprite)
    {
        if (!sprite || !sprite.gameObject)
        {
            Debug.LogError("[BaseManager] ClickFlash: Sprite = null!");
            yield break;
        }

        Debug.Log("[BaseManager] ClickFlash: Анимация...");
        Vector3 original = sprite.transform.localScale;
        sprite.transform.localScale = original * 1.1f;
        sprite.color = new Color(1.5f, 1.5f, 1.5f);
        yield return new WaitForSecondsRealtime(0.08f);

        if (sprite != null && sprite.gameObject != null)
        {
            sprite.transform.localScale = original;
            sprite.color = Color.white;
            Debug.Log("[BaseManager] ClickFlash: Анимация завершена");
        }
    }

    public void ShowMessage(string text, float duration = MESSAGE_DURATION)
    {
        Debug.Log($"[BaseManager] ShowMessage: \"{text}\" | Длительность: {duration}");
        if (messageCoroutine != null) StopCoroutine(messageCoroutine);
        messageCoroutine = StartCoroutine(ShowMessageRoutine(text, duration));
    }

    private IEnumerator ShowMessageRoutine(string text, float duration)
    {
        if (!messagePanel)
        {
            Debug.LogError("[BaseManager] messagePanel = null!");
            yield break;
        }
        if (!messageText)
        {
            Debug.LogError("[BaseManager] messageText = null!");
            yield break;
        }

        messagePanel.SetActive(true);
        messageText.text = text;
        Debug.Log($"[BaseManager] Сообщение показано: {text}");

        yield return new WaitForSecondsRealtime(duration);

        messagePanel.SetActive(false);
        Debug.Log("[BaseManager] Сообщение скрыто");
        messageCoroutine = null;
    }

    public void UpdateLabels()
    {
        Debug.Log("[BaseManager] UpdateLabels() вызван");

        if (!foodLabel) Debug.LogWarning("[BaseManager] foodLabel = null!");
        else foodLabel.text = $"Еда: {Globals.Food}";

        if (!medsLabel) Debug.LogWarning("[BaseManager] medsLabel = null!");
        else medsLabel.text = $"Медицина: {Globals.Meds}";

        if (!ammoLabel) Debug.LogWarning("[BaseManager] ammoLabel = null!");
        else ammoLabel.text = $"Боеприпасы: {Globals.Ammo}";

        if (!metalLabel) Debug.LogWarning("[BaseManager] metalLabel = null!");
        else metalLabel.text = $"Металл: {Globals.Metal}";

        if (!fuelLabel) Debug.LogWarning("[BaseManager] fuelLabel = null!");
        else fuelLabel.text = $"Топливо: {Globals.Fuel}";

        if (!waterLabel) Debug.LogWarning("[BaseManager] waterLabel = null!");
        else waterLabel.text = $"Вода: {Globals.Water}";

        if (!baseHpLabel) Debug.LogWarning("[BaseManager] baseHpLabel = null!");
        else baseHpLabel.text = $"База: {Globals.BaseHP}";

        if (!survivorsLabel) Debug.LogWarning("[BaseManager] survivorsLabel = null!");
        else survivorsLabel.text = $"Выжившие: {Globals.Survivors}";

        Debug.Log($"[BaseManager] Лейблы обновлены: Еда={Globals.Food}, Выжившие={Globals.Survivors}");
    }
}