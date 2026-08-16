using System;
using System.Collections.Generic;

[Serializable]
public class InventoryModel : ModelAbstract
{
    private readonly List<InventoryItemId> items = new();
    public BindableProperty<InventoryItemId> SelectedItem { get; private set; }
    public IReadOnlyList<InventoryItemId> Items => items;
    protected override void OnInit()
    {
        items.Clear();
        SelectedItem = new BindableProperty<InventoryItemId>(InventoryItemId.None);
    }
    public bool HasItem(InventoryItemId itemId)
    {
        return itemId != InventoryItemId.None && items.Contains(itemId);
    }
    public bool AddItem(InventoryItemId itemId)
    {
        if (itemId == InventoryItemId.None) return false;
        if (items.Contains(itemId)) return false;

        items.Add(itemId);
        return true;
    }
    public bool RemoveItem(InventoryItemId itemId)
    {
        if (!items.Remove(itemId)) return false;
        if(SelectedItem.Value == itemId)
        {
            SelectedItem.Value = items.Count > 0 ? items[0] : InventoryItemId.None;
        }
        return true;
    }
    public void Clear()
    {
        items.Clear();
        SelectedItem.Value = InventoryItemId.None;
    }

    public void Restore(IEnumerable<InventoryItemId> restoredItems, InventoryItemId selectedItem)
    {
        items.Clear();
        if (restoredItems != null)
        {
            foreach (InventoryItemId itemId in restoredItems)
            {
                if (itemId != InventoryItemId.None && !items.Contains(itemId))
                {
                    items.Add(itemId);
                }
            }
        }

        InventoryItemId resolvedSelection =
            selectedItem != InventoryItemId.None && items.Contains(selectedItem)
                ? selectedItem
                : items.Count > 0 ? items[0] : InventoryItemId.None;
        SelectedItem.SetValueWithoutNotify(resolvedSelection);
    }
    public bool SelectItem(InventoryItemId itemId)
    {
        if (itemId == InventoryItemId.None) return false;
        if (itemId != InventoryItemId.None && !items.Contains(itemId)) return false;
        if (SelectedItem.Value == itemId) return false;
        SelectedItem.Value = itemId;
        return true;
    }
}
