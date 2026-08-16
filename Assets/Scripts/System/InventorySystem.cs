using System.Collections.Generic;
using UnityEngine;

public class InventorySystem : SystemAbstract
{
    private InventoryModel inventoryModel;
    protected override void OnInit()
    {
        inventoryModel = this.GetModel<InventoryModel>();
    }
    public IReadOnlyList<InventoryItemId> GetItems()
    {
        return inventoryModel != null ? inventoryModel.Items : null;
    }
    public bool HasItem(InventoryItemId itemId)
    {
        return inventoryModel != null && inventoryModel.HasItem(itemId);
    }
    public bool AddItem(InventoryItemId itemId, bool selectAfterAdd = false)
    {
        if (inventoryModel == null) return false;
        if (!inventoryModel.AddItem(itemId)) return false;
        if(selectAfterAdd)
        {
            inventoryModel.SelectItem(itemId);
        }
        PublishChanged();
        this.GetEvent().Send(new InventoryItemAddedEvent { ItemId = itemId });
        return true;
    }
    public bool RemoveItem(InventoryItemId itemId)
    {
        if (inventoryModel == null) return false;
        if (!inventoryModel.RemoveItem(itemId)) return false;

        PublishChanged();
        this.GetEvent().Send(new InventoryItemRemovedEvent { ItemId = itemId });
        return true;
    }

    public bool SelectItem(InventoryItemId itemId)
    {
        if (inventoryModel == null) return false;
        if (!inventoryModel.SelectItem(itemId)) return false;

        PublishChanged();
        this.GetEvent().Send(new InventorySelectionChangedEvent { ItemId = itemId });
        return true;
    }

    public void Clear()
    {
        if (inventoryModel == null) return;

        inventoryModel.Clear();
        PublishChanged();
    }

    public void Restore(IEnumerable<InventoryItemId> items, InventoryItemId selectedItem)
    {
        if (inventoryModel == null) return;

        inventoryModel.Restore(items, selectedItem);
        PublishChanged();
    }

    private void PublishChanged()
    {
        if (inventoryModel == null) return;

        this.GetEvent().Send(new InventoryChangedEvent
        {
            Items = inventoryModel.Items.Count == 0
                ? System.Array.Empty<InventoryItemId>()
                : new List<InventoryItemId>(inventoryModel.Items).ToArray(),
            SelectedItem = inventoryModel.SelectedItem.Value
        });
    }
}

