using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class InventoryGrid
{
    public Vector2Int gridDimensions;
    public InventoryItem[,] grid;
    public List<InventoryItem> items;

    public InventoryGrid(int width, int height)
    {
        gridDimensions = new Vector2Int(width, height);
        grid = new InventoryItem[width, height];
        items = new List<InventoryItem>();
    }

    public bool CanPlaceItem(InventoryItem item, Vector2Int position)
    {
        if (item == null) return false;

        // Check if item fits within grid bounds
        if (position.x + item.gridSize.x > gridDimensions.x ||
            position.y + item.gridSize.y > gridDimensions.y ||
            position.x < 0 || position.y < 0)
            return false;

        // Check if all required slots are empty
        for (int x = position.x; x < position.x + item.gridSize.x; x++)
        {
            for (int y = position.y; y < position.y + item.gridSize.y; y++)
            {
                if (grid[x, y] != null && grid[x, y] != item)
                    return false;
            }
        }

        return true;
    }

    public bool PlaceItem(InventoryItem item, Vector2Int position)
    {
        if (!CanPlaceItem(item, position))
            return false;

        // Place item in all required slots
        for (int x = position.x; x < position.x + item.gridSize.x; x++)
        {
            for (int y = position.y; y < position.y + item.gridSize.y; y++)
            {
                grid[x, y] = item;
            }
        }

        if (!items.Contains(item))
            items.Add(item);

        return true;
    }

    public void RemoveItem(InventoryItem item)
    {
        // Clear all grid slots occupied by this item
        for (int x = 0; x < gridDimensions.x; x++)
        {
            for (int y = 0; y < gridDimensions.y; y++)
            {
                if (grid[x, y] == item)
                    grid[x, y] = null;
            }
        }

        items.Remove(item);
    }

    public Vector2Int? FindEmptySpace(InventoryItem item)
    {
        for (int y = 0; y <= gridDimensions.y - item.gridSize.y; y++)
        {
            for (int x = 0; x <= gridDimensions.x - item.gridSize.x; x++)
            {
                if (CanPlaceItem(item, new Vector2Int(x, y)))
                    return new Vector2Int(x, y);
            }
        }
        return null;
    }
}

