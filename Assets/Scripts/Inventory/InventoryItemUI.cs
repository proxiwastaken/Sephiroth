using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class InventoryItemUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public Image itemIcon;
    public TextMeshProUGUI stackText;
    public Image backgroundImage;
    public Color normalColor = new Color(1, 1, 1, 0.8f);
    public Color draggingColor = new Color(1, 1, 1, 0.5f);

    private InventoryItem item;
    private Vector2Int gridPosition;
    private InventorySystem inventorySystem;
    private Canvas parentCanvas;
    private bool isDragging = false;
    private Vector3 originalPosition;
    private CanvasGroup canvasGroup;
    private Vector2 dragOffset;

    public void Initialize(InventoryItem inventoryItem, Vector2Int position, InventorySystem system)
    {
        item = inventoryItem;
        gridPosition = position;
        inventorySystem = system;
        parentCanvas = GetComponentInParent<Canvas>();

        SetupUI();
        UpdateDisplay();
    }

    void SetupUI()
    {
        // Try to find UI components by name first
        Transform iconTransform = transform.Find("Icon");
        if (iconTransform != null)
            itemIcon = iconTransform.GetComponent<Image>();

        Transform stackTransform = transform.Find("StackText");
        if (stackTransform != null)
            stackText = stackTransform.GetComponent<TextMeshProUGUI>();

        if (backgroundImage == null)
            backgroundImage = GetComponent<Image>();

        // Add CanvasGroup if it doesn't exist
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        originalPosition = transform.localPosition;
    }

    void UpdateDisplay()
    {
        if (item == null) return;

        // Set icon with proper fallback
        if (itemIcon != null)
        {
            if (item.icon != null)
            {
                itemIcon.sprite = item.icon;
                itemIcon.color = Color.white;
                itemIcon.gameObject.SetActive(true);
            }
            else
            {
                // No icon available - show colored square instead
                itemIcon.sprite = null;
                itemIcon.color = GetRarityColor(item.mushroomData.rarity);
                itemIcon.gameObject.SetActive(true);

                // Create a simple white texture for the icon if none exists
                if (itemIcon.sprite == null)
                {
                    itemIcon.sprite = CreateDefaultSprite();
                }
            }
        }

        // Set stack text
        if (stackText != null)
        {
            if (item.currentStack > 1)
            {
                stackText.text = item.currentStack.ToString();
                stackText.gameObject.SetActive(true);
            }
            else
            {
                stackText.gameObject.SetActive(false);
            }
        }

        // Set size based on item grid size
        RectTransform rect = GetComponent<RectTransform>();
        if (rect != null)
        {
            float slotSize = inventorySystem.slotSize;
            float spacing = inventorySystem.slotSpacing;

            Vector2 size = new Vector2(
                item.gridSize.x * slotSize + (item.gridSize.x - 1) * spacing,
                item.gridSize.y * slotSize + (item.gridSize.y - 1) * spacing
            );

            rect.sizeDelta = size;
        }

        // Set background color based on rarity
        if (backgroundImage != null && item.mushroomData != null)
        {
            Color rarityColor = GetRarityColor(item.mushroomData.rarity);
            backgroundImage.color = rarityColor;
        }
    }

    // NEW: Create a default sprite for items without icons
    Sprite CreateDefaultSprite()
    {
        // Create a simple 1x1 white texture
        Texture2D tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();

        return Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));
    }

    Color GetRarityColor(MushroomRarity rarity)
    {
        return rarity switch
        {
            MushroomRarity.Common => new Color(0.8f, 0.8f, 0.8f, 0.8f),
            MushroomRarity.Uncommon => new Color(0.4f, 1f, 0.4f, 0.8f),
            MushroomRarity.Rare => new Color(0.4f, 0.4f, 1f, 0.8f),
            MushroomRarity.Epic => new Color(1f, 0.4f, 1f, 0.8f),
            MushroomRarity.Legendary => new Color(1f, 0.8f, 0.2f, 0.8f),
            _ => Color.white
        };
    }

    public void SetDragging(bool dragging)
    {
        isDragging = dragging;

        if (canvasGroup != null)
        {
            if (dragging)
            {
                canvasGroup.alpha = 0.6f; // Semi-transparent when dragging
                canvasGroup.blocksRaycasts = false; // Allow dropping
            }
            else
            {
                canvasGroup.alpha = 1.0f; // Full opacity when not dragging
                canvasGroup.blocksRaycasts = true; // Normal interaction
            }
        }

        // Keep the background color but make it more transparent when dragging
        if (backgroundImage != null && item?.mushroomData != null)
        {
            Color baseColor = GetRarityColor(item.mushroomData.rarity);
            if (dragging)
            {
                backgroundImage.color = new Color(baseColor.r, baseColor.g, baseColor.b, 0.5f);
            }
            else
            {
                backgroundImage.color = baseColor;
            }
        }

        if (dragging)
        {
            transform.SetAsLastSibling(); // Bring to front
        }
        else
        {
            transform.localPosition = originalPosition; // Return to original position
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        Debug.Log($"📦 Starting drag of {item?.displayName}");

        RectTransform rectTransform = transform as RectTransform;
        RectTransform canvasRectTransform = parentCanvas.transform as RectTransform;

        // Calculate the offset between mouse position and item center IN CANVAS SPACE
        Vector2 canvasMousePosition;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRectTransform,
            eventData.position,
            parentCanvas.worldCamera,
            out canvasMousePosition))
        {
            // Get the item's position in canvas space
            Vector2 itemPositionInCanvas = rectTransform.localPosition;

            // Calculate offset
            dragOffset = canvasMousePosition - itemPositionInCanvas;
        }

        inventorySystem.StartDragging(this, item, gridPosition);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (isDragging && parentCanvas != null)
        {
            RectTransform rectTransform = transform as RectTransform;

            // Convert screen position to local position in the parent canvas
            Vector2 localMousePosition;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentCanvas.transform as RectTransform,
                eventData.position,
                parentCanvas.worldCamera,
                out localMousePosition))
            {
                // Apply the drag offset to keep the item positioned correctly relative to the mouse
                rectTransform.localPosition = localMousePosition - dragOffset;
            }
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        Debug.Log($"📦 Ending drag of {item?.displayName}");

        // Find what slot we're over
        Vector2Int targetSlot = GetSlotFromPosition(eventData.position);
        inventorySystem.StopDragging(targetSlot);
    }

    // NEW: Helper method to determine which slot we're dropping on
    Vector2Int GetSlotFromPosition(Vector2 screenPosition)
    {
        // Cast a ray to find which slot we're over
        PointerEventData tempEventData = new PointerEventData(EventSystem.current);
        tempEventData.position = screenPosition;

        var results = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(tempEventData, results);

        foreach (var result in results)
        {
            var slot = result.gameObject.GetComponent<InventorySlot>();
            if (slot != null)
            {
                // Return the slot's grid position
                return slot.gridPosition; // You'll need to make this public in InventorySlot
            }
        }

        // If no slot found, return the original position
        return gridPosition;
    }
}