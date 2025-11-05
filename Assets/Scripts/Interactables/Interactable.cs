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

        // === БАЗА ===
        if (scene == "BaseScene")
        {
            if (BaseManager.Instance != null)
            {
                BaseManager.Instance.OnItemClicked(this);
            }
            else
            {
                Debug.LogWarning("BaseManager.Instance = null в BaseScene!");
            }
        }

        // === РЕЙД ===
        else if (scene == "Raid" && RaidManager.Instance != null)
        {
            if (itemType == BaseManager.ItemType.Zombie)
            {
                RaidManager.Instance.OnZombieClicked(gameObject);
            }
            else if (itemType == BaseManager.ItemType.GoBase)
            {
                RaidManager.Instance.OnGoBaseClicked(gameObject);
            }
            else if (itemType == BaseManager.ItemType.Raid)
            {
                // ПЕРЕХОД В РЕЙД — ИЗ БАЗЫ
                // Это НЕ должно быть в рейде!
                // Но если объект "Raid" в рейде — игнорируем
                if (BaseManager.Instance != null)
                {
                    BaseManager.Instance.OnItemClicked(this);
                }
            }
        }
    }
}