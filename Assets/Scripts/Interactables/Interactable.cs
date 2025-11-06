using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(SpriteRenderer))]
public class Interactable : MonoBehaviour
{
    public BaseManager.ItemType itemType = BaseManager.ItemType.Garden;
    [HideInInspector] public SpriteRenderer Sprite;

    private void Awake()
    {
        Sprite = GetComponent<SpriteRenderer>();
    }

    private void OnMouseDown()
    {
        string scene = SceneManager.GetActiveScene().name;

        // === ТОЛЬКО В BaseScene ===
        if (scene == "BaseScene")
        {
            BaseManager baseManager = FindAnyObjectByType<BaseManager>();
            if (baseManager != null)
            {
                baseManager.OnItemClicked(this);
            }
            else
            {
                Debug.LogError("BaseManager не найден на сцене BaseScene!");
            }
        }
        // === ТОЛЬКО В Raid ===
        else if (scene == "Raid")
        {
            RaidManager raidManager = FindAnyObjectByType<RaidManager>();
            if (raidManager == null)
            {
                Debug.LogError("RaidManager не найден на сцене Raid!");
                return;
            }

            if (itemType == BaseManager.ItemType.Zombie)
            {
                raidManager.OnZombieClicked(gameObject);
            }
            else if (itemType == BaseManager.ItemType.GoBase)
            {
                raidManager.OnGoBaseClicked(gameObject);
            }
            // Остальные типы в рейде игнорируются — как и задумано
        }
    }
}