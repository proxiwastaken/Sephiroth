using UnityEngine;
using System.Collections.Generic;
using System;

[System.Serializable]
public class InventoryItem
{
    public string itemId;
    public string displayName;
    public Sprite icon;
    public MushroomData mushroomData; // Reference to your existing MushroomData
    public ItemType itemType;
    public int stackSize;
    public int currentStack;
    public Vector2Int gridSize; // Width x Height in grid slots
    public string description;

    [Header("Actions")]
    public bool canEquip = false;
    public bool canEat = true;
    public bool canDrop = true;

    public InventoryItem(MushroomData mushroom)
    {
        mushroomData = mushroom;
        itemId = mushroom.mushroomType;
        displayName = mushroom.displayName;
        itemType = ItemType.Mushroom;
        stackSize = GetStackSizeForRarity(mushroom.rarity);
        currentStack = 1;
        gridSize = GetGridSizeForRarity(mushroom.rarity);
        description = $"A {mushroom.rarity} mushroom.";
        canEat = true;
        canDrop = true;
        canEquip = false;
    }

    private int GetStackSizeForRarity(MushroomRarity rarity)
    {
        return rarity switch
        {
            MushroomRarity.Common => 10,
            MushroomRarity.Uncommon => 8,
            MushroomRarity.Rare => 5,
            MushroomRarity.Epic => 3,
            MushroomRarity.Legendary => 1,
            _ => 1
        };
    }

    private Vector2Int GetGridSizeForRarity(MushroomRarity rarity)
    {
        return rarity switch
        {
            MushroomRarity.Common => new Vector2Int(1, 1),
            MushroomRarity.Uncommon => new Vector2Int(1, 1),
            MushroomRarity.Rare => new Vector2Int(1, 2),
            MushroomRarity.Epic => new Vector2Int(2, 1),
            MushroomRarity.Legendary => new Vector2Int(2, 2),
            _ => new Vector2Int(1, 1)
        };
    }
}

public enum ItemType
{
    Mushroom,
    Equipment,
    Consumable,
    Key
}