using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class InventorySlot : MonoBehaviour, IDropHandler, IPointerClickHandler
{
    public Image backgroundImage;
    public Color normalColor = Color.white;
    public Color occupiedColor = Color.gray;
    public Color highlightColor = Color.yellow;

    public Vector2Int gridPosition { get; private set; }
    private InventorySystem inventorySystem;
    private InventoryItemUI currentItemUI;

    public void Initialize(Vector2Int position, InventorySystem system)
    {
        gridPosition = position;
        inventorySystem = system;

        if (backgroundImage == null)
            backgroundImage = GetComponent<Image>();

        SetOccupied(false);
    }

    public void SetItem(InventoryItem item)
    {
        ClearItem();

        if (item != null)
        {
            // Create item UI
            GameObject itemObj = Instantiate(inventorySystem.itemUIPrefab, transform);
            currentItemUI = itemObj.GetComponent<InventoryItemUI>();
            if (currentItemUI == null)
                currentItemUI = itemObj.AddComponent<InventoryItemUI>();

            currentItemUI.Initialize(item, gridPosition, inventorySystem);
            SetOccupied(true);
        }
    }

    public void ClearItem()
    {
        if (currentItemUI != null)
        {
            Destroy(currentItemUI.gameObject);
            currentItemUI = null;
        }
        SetOccupied(false);
    }

    public void SetOccupied(bool occupied)
    {
        if (backgroundImage != null)
            backgroundImage.color = occupied ? occupiedColor : normalColor;
    }

    public void OnDrop(PointerEventData eventData)
    {
        inventorySystem.StopDragging(gridPosition);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        InventoryItem item = inventorySystem.GetItemAt(gridPosition);
        if (item != null)
        {
            inventorySystem.SelectItem(item);
        }
        else
        {
            inventorySystem.DeselectItem();
        }
    }
}
