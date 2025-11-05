using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

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
    private int ammo, survivors;
    private bool started;
    private float lastClickTime;
    private Coroutine messageCoroutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        sfx = gameObject.AddComponent<AudioSource>();
        if (messagePanel) messagePanel.SetActive(false);

        // ПОДПИСКА НА ЗАГРУЗКУ СЦЕНЫ
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        // ОТПИСКА
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start() => StartRaid();

    private void StartRaid()
    {
        survivors = Mathf.Clamp(Random.Range(1, Globals.Survivors + 1), 1, Globals.Survivors);
        int costFood = survivors * Random.Range(1, 4);
        int costFuel = survivors * Random.Range(1, 4);

        Globals.AddFood(-costFood);
        Globals.AddFuel(-costFuel);
        ammo = Mathf.Min(Globals.Ammo, survivors * 10);

        started = true;
        UpdateUI();
        ShowMsg($"Рейд начат: {survivors} выживших | -{costFood} еды, -{costFuel} топлива", 4f);
    }

    private void UpdateUI()
    {
        int alive = 0;
        foreach (GameObject z in GameObject.FindGameObjectsWithTag("Zombie"))
            if (z != null && z.activeInHierarchy) alive++;

        if (zombieLabel) zombieLabel.text = $"Зомби: {alive}";
        if (ammoLabel) ammoLabel.text = $"Боеприпасы: {ammo}";
        if (survivorsLabel) survivorsLabel.text = $"Выживших: {survivors}";
    }

    public void OnZombieClicked(GameObject zombie)
    {
        if (!started || Time.realtimeSinceStartup - lastClickTime < 0.1f) return;
        lastClickTime = Time.realtimeSinceStartup;

        if (ammo <= 0)
        {
            ShowMsg("Нет патронов! Вернитесь на базу!", 3f);
            return;
        }

        ammo--;
        sfx.PlayOneShot(clickSound);
        StartCoroutine(KillZombie(zombie));
    }

    public void OnGoBaseClicked(GameObject obj)
    {
        if (!started || Time.realtimeSinceStartup - lastClickTime < 0.1f) return;
        lastClickTime = Time.realtimeSinceStartup;

        sfx.PlayOneShot(clickSound);
        Retreat();
    }

    private IEnumerator KillZombie(GameObject zombie)
    {
        var sr = zombie.GetComponent<SpriteRenderer>();
        if (sr)
        {
            Vector3 orig = sr.transform.localScale;
            sr.transform.localScale *= 1.1f;
            sr.color = new Color(1.5f, 1.5f, 1.5f);
            yield return new WaitForSecondsRealtime(0.1f);

            if (sr && zombie)
            {
                sr.transform.localScale = orig;
                sr.color = Color.white;
            }
        }

        yield return new WaitForSecondsRealtime(0.05f);
        if (zombie) Destroy(zombie);
        UpdateUI();

        if (CountAliveZombies() == 0)
            StartCoroutine(Victory());
    }

    private int CountAliveZombies()
    {
        int count = 0;
        foreach (GameObject z in GameObject.FindGameObjectsWithTag("Zombie"))
            if (z != null && z.activeInHierarchy) count++;
        return count;
    }

    private IEnumerator Victory()
    {
        int food = survivors * Random.Range(2, 6);
        int metal = survivors * Random.Range(1, 5);
        int rewardAmmo = survivors * Random.Range(1, 4);

        Globals.AddFood(food);
        Globals.AddMetal(metal);
        Globals.AddAmmo(ammo + rewardAmmo);
        Globals.AddBaseHP(survivors);

        ShowMsg($"Победа! +{food} еды, +{metal} металла, +{rewardAmmo} патронов", 4f);
        yield return new WaitForSecondsRealtime(4f);
        ReturnToBase();
    }

    private void Retreat()
    {
        int maxLoss = Mathf.Max(1, survivors / 10);
        int dead = Random.Range(0, maxLoss + 1);
        int wounded = Random.Range(0, maxLoss - dead + 1);
        int healed = Mathf.Min(wounded, Globals.Meds);
        int totalLoss = dead + (wounded - healed);

        Globals.AddSurvivors(-totalLoss);
        Globals.AddMeds(-healed);

        int used = Mathf.Min(Globals.Ammo, survivors * 10) - ammo;
        Globals.SetAmmo(Globals.Ammo - used);

        ShowMsg($"Отступление! Погибло: {dead}, ранено: {wounded - healed}", 4f);
        StartCoroutine(DelayedReturn(4f));
    }

    private IEnumerator DelayedReturn(float t)
    {
        yield return new WaitForSecondsRealtime(t);
        ReturnToBase();
    }

    private void ReturnToBase()
    {
        if (SceneManager.sceneCountInBuildSettings > 0)
        {
            SceneManager.LoadScene(0); // BaseScene
        }
        else
        {
            Debug.LogError("Нет сцен в Build Settings!");
        }
    }

    // РАБОТАЕТ — ПОТОМУ ЧТО UpdateLabels() public!
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "BaseScene")
        {
            BaseManager.Instance?.UpdateLabels();
        }
    }

    private void ShowMsg(string text, float duration = 3f)
    {
        if (messageCoroutine != null) StopCoroutine(messageCoroutine);
        messageCoroutine = StartCoroutine(MsgRoutine(text, duration));
    }

    private IEnumerator MsgRoutine(string text, float t)
    {
        if (messagePanel) messagePanel.SetActive(true);
        if (messageText) messageText.text = text;

        yield return new WaitForSecondsRealtime(t);

        if (messagePanel) messagePanel.SetActive(false);
        messageCoroutine = null;
    }
}