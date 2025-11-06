using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class RaidManager : MonoBehaviour
{
    public static RaidManager Instance { get; private set; }

    [Header("UI")]
    public TMP_Text zombieLabel, ammoLabel, survivorsLabel;

    [Header("FX")]
    public AudioClip clickSound;
    public GameObject messagePanel;
    public TMP_Text messageText;

    private AudioSource sfx;
    private int localAmmo, raidSurvivors, baseAmmoBefore;
    private bool started;
    private float lastClickTime;
    private Coroutine messageCoroutine;

    private readonly List<GameObject> aliveZombies = new List<GameObject>();

    private void Awake()
    {
        Debug.Log($"[RaidManager] Awake: Instance = {(Instance != null ? "УЖЕ ЕСТЬ" : "СОЗДАЁТСЯ")}");
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[RaidManager] ДУБЛИКАТ! Уничтожаем.");
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        Debug.Log("[RaidManager] Синглтон создан. DontDestroyOnLoad.");

        sfx = gameObject.AddComponent<AudioSource>();
        Debug.Log($"[RaidManager] AudioSource добавлен: {sfx != null}");

        if (messagePanel)
        {
            messagePanel.SetActive(false);
            Debug.Log("[RaidManager] messagePanel деактивирован.");
        }
        else
        {
            Debug.LogWarning("[RaidManager] messagePanel НЕ НАЗНАЧЕН!");
        }

        SceneManager.sceneLoaded += OnSceneLoaded;
        Debug.Log("[RaidManager] Подписка на sceneLoaded.");
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        Debug.Log("[RaidManager] Отписка от sceneLoaded. Уничтожен.");
    }

    private void Start()
    {
        Debug.Log("[RaidManager] Start() → StartRaid()");
        StartRaid();
    }

    private void StartRaid()
    {
        if (started)
        {
            Debug.LogWarning("[RaidManager] StartRaid(): Рейд уже запущен!");
            return;
        }

        Debug.Log($"[RaidManager] Начало рейда: Выживших = {Globals.Survivors}, Еда = {Globals.Food}, Топливо = {Globals.Fuel}");
        raidSurvivors = Mathf.Clamp(Random.Range(1, Globals.Survivors + 1), 1, Globals.Survivors);
        int costFood = raidSurvivors * Random.Range(1, 4);
        int costFuel = raidSurvivors * Random.Range(1, 4);

        Globals.AddFood(-costFood);
        Globals.AddFuel(-costFuel);
        Debug.Log($"[RaidManager] Списано: -{costFood} еды, -{costFuel} топлива");

        baseAmmoBefore = Globals.Ammo;
        localAmmo = Mathf.Min(Globals.Ammo, raidSurvivors * 10);
        Globals.SetAmmo(Globals.Ammo - localAmmo);
        Debug.Log($"[RaidManager] Патроны: Было {baseAmmoBefore} → В рейде: {localAmmo} → Осталось в базе: {Globals.Ammo}");

        started = true;

        aliveZombies.Clear();
        var zombies = GameObject.FindGameObjectsWithTag("Zombie");
        Debug.Log($"[RaidManager] Найдено зомби: {zombies.Length}");
        foreach (GameObject z in zombies)
        {
            if (z != null && z.activeInHierarchy)
            {
                aliveZombies.Add(z);
                Debug.Log($"[RaidManager] Зомби добавлен: {z.name}");
            }
        }

        UpdateUI();
        ShowMsg($"Рейд начат: {raidSurvivors} выживших | -{costFood} еды, -{costFuel} топлива", 4f);
    }

    private void UpdateUI()
    {
        Debug.Log("[RaidManager] UpdateUI() вызван");

        if (!zombieLabel) Debug.LogWarning("[RaidManager] zombieLabel = null!");
        else zombieLabel.text = $"Зомби: {aliveZombies.Count}";

        if (!ammoLabel) Debug.LogWarning("[RaidManager] ammoLabel = null!");
        else ammoLabel.text = $"Боеприпасы: {localAmmo}";

        if (!survivorsLabel) Debug.LogWarning("[RaidManager] survivorsLabel = null!");
        else survivorsLabel.text = $"Выживших: {raidSurvivors}";

        Debug.Log($"[RaidManager] UI обновлён: Зомби={aliveZombies.Count}, Патроны={localAmmo}, Выжившие={raidSurvivors}");
    }

    public void OnZombieClicked(GameObject zombie)
    {
        Debug.Log($"[RaidManager] OnZombieClicked: {zombie.name}");

        if (!started)
        {
            Debug.LogWarning("[RaidManager] Рейд не начат!");
            return;
        }
        if (Time.realtimeSinceStartup - lastClickTime < 0.1f)
        {
            Debug.LogWarning("[RaidManager] Клик слишком быстро!");
            return;
        }
        lastClickTime = Time.realtimeSinceStartup;

        if (localAmmo <= 0)
        {
            Debug.Log("[RaidManager] Нет патронов!");
            ShowMsg("Нет патронов! Вернитесь на базу!", 3f);
            return;
        }

        if (!aliveZombies.Contains(zombie))
        {
            Debug.LogWarning($"[RaidManager] Зомби {zombie.name} уже мёртв или не в списке!");
            return;
        }

        localAmmo--;
        Debug.Log($"[RaidManager] Выстрел: Патроны = {localAmmo}");
        sfx.PlayOneShot(clickSound);

        StartCoroutine(KillZombieWithAnimation(zombie));
    }

    private IEnumerator KillZombieWithAnimation(GameObject zombie)
    {
        var sr = zombie.GetComponent<SpriteRenderer>();
        if (!sr)
        {
            Debug.LogError($"[RaidManager] У {zombie.name} нет SpriteRenderer!");
            yield break;
        }

        Debug.Log($"[RaidManager] Анимация смерти: {zombie.name}");
        Vector3 orig = sr.transform.localScale;
        sr.transform.localScale *= 1.1f;
        sr.color = new Color(1.5f, 1.5f, 1.5f);
        yield return new WaitForSecondsRealtime(0.1f);

        if (sr && zombie)
        {
            sr.transform.localScale = orig;
            sr.color = Color.white;
        }

        yield return new WaitForSecondsRealtime(0.05f);

        if (zombie)
        {
            aliveZombies.Remove(zombie);
            Debug.Log($"[RaidManager] Зомби уничтожен: {zombie.name} | Осталось: {aliveZombies.Count}");
            Destroy(zombie);
        }

        UpdateUI();

        if (aliveZombies.Count == 0)
        {
            Debug.Log("[RaidManager] Все зомби убиты → Победа!");
            StartCoroutine(Victory());
        }
    }

    public void OnGoBaseClicked(GameObject obj)
    {
        Debug.Log($"[RaidManager] OnGoBaseClicked: {obj.name}");
        if (!started || Time.realtimeSinceStartup - lastClickTime < 0.1f) return;
        lastClickTime = Time.realtimeSinceStartup;

        sfx.PlayOneShot(clickSound);
        Debug.Log("[RaidManager] Отступление!");
        Retreat();
    }

    private IEnumerator Victory()
    {
        Debug.Log("[RaidManager] Победа! Расчёт награды...");
        int food = raidSurvivors * Random.Range(2, 6);
        int metal = raidSurvivors * Random.Range(1, 5);
        int rewardAmmo = raidSurvivors * Random.Range(1, 4);

        Globals.AddFood(food);
        Globals.AddMetal(metal);
        Globals.AddAmmo(localAmmo + rewardAmmo);
        Globals.AddBaseHP(raidSurvivors);

        Debug.Log($"[RaidManager] Награда: +{food} еды, +{metal} металла, +{rewardAmmo} патронов, +{raidSurvivors} HP");
        ShowMsg($"Победа! +{food} еды, +{metal} металла, +{rewardAmmo} патронов", 4f);
        yield return new WaitForSecondsRealtime(4f);
        ReturnToBase();
    }

    private void Retreat()
    {
        Debug.Log("[RaidManager] Отступление: Расчёт потерь...");
        int maxLoss = Mathf.Max(1, raidSurvivors / 10);
        int dead = Random.Range(0, maxLoss + 1);
        int wounded = Random.Range(0, maxLoss - dead + 1);
        int healed = Mathf.Min(wounded, Globals.Meds);
        int totalLoss = dead + (wounded - healed);

        Globals.AddSurvivors(-totalLoss);
        Globals.AddMeds(-healed);

        int used = baseAmmoBefore - localAmmo;
        Globals.SetAmmo(Globals.Ammo - used);

        Debug.Log($"[RaidManager] Потери: Погибло={dead}, Ранено={wounded - healed}, Вылечено={healed}, Всего потерь={totalLoss}");
        ShowMsg($"Отступление! Погибло: {dead}, ранено: {wounded - healed}", 4f);
        StartCoroutine(DelayedReturn(4f));
    }

    private IEnumerator DelayedReturn(float t)
    {
        Debug.Log($"[RaidManager] Задержка возврата: {t} сек");
        yield return new WaitForSecondsRealtime(t);
        ReturnToBase();
    }

    private void ReturnToBase()
    {
        Debug.Log("[RaidManager] Возврат на базу → Загрузка BaseScene");
        SceneManager.LoadScene("BaseScene"); // ← ТОЧНО BaseScene!
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"[RaidManager] OnSceneLoaded: {scene.name} | Mode: {mode}");
        if (scene.name == "BaseScene")
        {
            Debug.Log("[RaidManager] BaseScene загружена → BaseManager.UpdateLabels()");
            BaseManager.Instance?.UpdateLabels();
        }
    }

    private void ShowMsg(string text, float duration = 3f)
    {
        Debug.Log($"[RaidManager] ShowMsg: \"{text}\" | Длительность: {duration}");
        if (messageCoroutine != null) StopCoroutine(messageCoroutine);
        messageCoroutine = StartCoroutine(MsgRoutine(text, duration));
    }

    private IEnumerator MsgRoutine(string text, float t)
    {
        if (!messagePanel)
        {
            Debug.LogError("[RaidManager] messagePanel = null!");
            yield break;
        }
        if (!messageText)
        {
            Debug.LogError("[RaidManager] messageText = null!");
            yield break;
        }

        messagePanel.SetActive(true);
        messageText.text = text;
        Debug.Log($"[RaidManager] Сообщение показано: {text}");

        yield return new WaitForSecondsRealtime(t);

        messagePanel.SetActive(false);
        Debug.Log("[RaidManager] Сообщение скрыто");
        messageCoroutine = null;
    }
}