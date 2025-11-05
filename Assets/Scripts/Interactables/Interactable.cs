using UnityEngine;

public class Interactable : MonoBehaviour
{
    public BaseManager.ItemType itemType;

    private void OnMouseDown()
    {
        BaseManager.Instance?.OnItemClicked(this);
    }

    public SpriteRenderer Sprite => GetComponent<SpriteRenderer>();
}