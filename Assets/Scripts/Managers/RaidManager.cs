using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class RaidManager : MonoBehaviour
{
    [Header("UI")]
    public TMP_Text zombieLabel;
    public TMP_Text ammoLabel;
    public TMP_Text survivorsLabel;

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
        sfx = gameObject.AddComponent<AudioSource>();
        if (messagePanel) messagePanel.SetActive(false);
    }

    private void Start()
    {
        StartRaid();
    }

    private void StartRaid()
    {
        if (started) return;

        raidSurvivors = Mathf.Clamp(Random.Range(1, Globals.Survivors + 1), 1, Globals.Survivors);

        int costFood = raidSurvivors * Random.Range(1, 4);
        int costFuel = raidSurvivors * Random.Range(1, 4);
        Globals.AddFood(-costFood);
        Globals.AddFuel(-costFuel);

        baseAmmoBefore = Globals.Ammo;
        localAmmo = Mathf.Min(Globals.Ammo, raidSurvivors * 10);
        Globals.SetAmmo(Globals.Ammo - localAmmo);

        started = true;
        aliveZombies.Clear();

        foreach (GameObject z in GameObject.FindGameObjectsWithTag("Zombie"))
        {
            if (z != null && z.activeInHierarchy)
                aliveZombies.Add(z);
        }

        UpdateUI();
        ShowMsg($"Рейд начат: {raidSurvivors} выживших | -{costFood} еды, -{costFuel} топлива", 4f);
    }

    private void UpdateUI()
    {
        if (zombieLabel) zombieLabel.text = $"Зомби: {aliveZombies.Count}";
        if (ammoLabel) ammoLabel.text = $"Боеприпасы: {localAmmo}";
        if (survivorsLabel) survivorsLabel.text = $"Выживших: {raidSurvivors}";
    }

    public void OnZombieClicked(GameObject zombie)
    {
        if (!started || Time.realtimeSinceStartup - lastClickTime < 0.1f) return;
        lastClickTime = Time.realtimeSinceStartup;

        if (localAmmo <= 0)
        {
            ShowMsg("Нет патронов! Вернитесь на базу!", 3f);
            return;
        }

        if (!aliveZombies.Contains(zombie)) return;

        localAmmo--;
        sfx.PlayOneShot(clickSound);
        StartCoroutine(KillZombieWithAnimation(zombie));
    }

    private IEnumerator KillZombieWithAnimation(GameObject zombie)
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

        if (zombie)
        {
            aliveZombies.Remove(zombie);
            Destroy(zombie);
        }

        UpdateUI();

        if (aliveZombies.Count == 0)
            StartCoroutine(Victory());
    }

    public void OnGoBaseClicked(GameObject obj)
    {
        if (!started || Time.realtimeSinceStartup - lastClickTime < 0.1f) return;
        lastClickTime = Time.realtimeSinceStartup;

        sfx.PlayOneShot(clickSound);
        Retreat();
    }

    private IEnumerator Victory()
    {
        int food = raidSurvivors * Random.Range(2, 6);
        int metal = raidSurvivors * Random.Range(1, 5);
        int rewardAmmo = raidSurvivors * Random.Range(1, 4);
        int baseHpGained = raidSurvivors;

        Globals.AddFood(food);
        Globals.AddMetal(metal);
        Globals.AddAmmo(localAmmo + rewardAmmo);
        Globals.AddBaseHP(baseHpGained);
        Globals.Save(); // ← СОХРАНЕНИЕ

        ShowMsg($"Победа! +{food} еды, +{metal} металла, +{rewardAmmo} боеприпасов, +{baseHpGained} прочности базы", 4f);
        yield return new WaitForSecondsRealtime(4f);
        ReturnToBase();
    }

    private void Retreat()
    {
        int maxLoss = Mathf.Max(1, raidSurvivors / 10);
        int dead = Random.Range(0, maxLoss + 1);
        int wounded = Random.Range(0, maxLoss - dead + 1);
        int healed = Mathf.Min(wounded, Globals.Meds);
        int totalLoss = dead + (wounded - healed);

        Globals.AddSurvivors(-totalLoss);
        Globals.AddMeds(-healed);

        int used = baseAmmoBefore - localAmmo;
        Globals.SetAmmo(Globals.Ammo - used);
        Globals.Save(); // ← СОХРАНЕНИЕ

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
        SceneManager.LoadScene("BaseScene");
    }

    private void ShowMsg(string text, float duration = 3f)
    {
        if (messageCoroutine != null) StopCoroutine(messageCoroutine);
        messageCoroutine = StartCoroutine(MsgRoutine(text, duration));
    }

    private IEnumerator MsgRoutine(string text, float t)
    {
        if (!messagePanel || !messageText) yield break;

        messagePanel.SetActive(true);
        messageText.text = text;
        yield return new WaitForSecondsRealtime(t);
        messagePanel.SetActive(false);
        messageCoroutine = null;
    }
}