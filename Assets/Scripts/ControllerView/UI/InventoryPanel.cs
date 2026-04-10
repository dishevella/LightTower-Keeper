using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventoryPanel : ModalPanelBase
{
    [Header("View")]
    [SerializeField] private GameObject rootObject;
    [SerializeField] private Transform itemListRoot;
    [SerializeField] private InventoryItemEntryUI itemEntryPrefab;
    [SerializeField] private TMP_Text selectedItemTitleText;
    [SerializeField] private TMP_Text selectedItemDescriptionText;
    [SerializeField] private TMP_Text actionButtonLabelText;
    [SerializeField] private Button actionButton;
    [SerializeField] private Button closeButton;

    [Header("Control")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private PlayerInteractionController playerInteractionController;
    [SerializeField] private KeyCode openInventoryKey = KeyCode.I;
    [SerializeField] private bool allowEscapeToClose = true;

    private readonly List<InventoryItemEntryUI> activeEntries = new();

    private InventoryModel inventoryModel;
    private InventorySystem inventorySystem;
    private InventoryItemId focusedItem = InventoryItemId.None;
    protected override GameObject RootObject => rootObject;
    protected override PlayerController ControlledPlayer => playerController;
    protected override PlayerInteractionController ControlledInteraction => playerInteractionController;
    protected override bool AllowEscapeToClose => allowEscapeToClose;
    protected override bool HasOpenShortcut => true;
    protected override KeyCode OpenShortcutKey => openInventoryKey;

    protected override void Awake()
    {
        base.Awake();

        if (actionButton != null)
        {
            actionButton.onClick.RemoveListener(HandleActionButtonClicked);
            actionButton.onClick.AddListener(HandleActionButtonClicked);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(ClosePanel);
            closeButton.onClick.AddListener(ClosePanel);
        }
    }

    private void Start()
    {
        inventoryModel = this.GetModel<InventoryModel>();
        inventorySystem = this.GetSystem<InventorySystem>();

        if (inventoryModel != null)
        {
            inventoryModel.SelectedItem.OnValueChanged += HandleSelectedItemChanged;
        }

        this.GetEvent().Register<InventoryChangedEvent>(HandleInventoryChanged)
            .UnRegisterWhenGameObjectDestroyed(gameObject);

        RefreshPanel();
    }

    protected override void OnDestroy()
    {
        if (inventoryModel != null)
        {
            inventoryModel.SelectedItem.OnValueChanged -= HandleSelectedItemChanged;
        }

        base.OnDestroy();
    }

    public bool OpenPanel()
    {
        if (!HasAnyItem()) return false;
        RefreshPanel();
        return OpenModalPanel();
    }

    private void HandleInventoryChanged(InventoryChangedEvent _)
    {
        RefreshPanel();
    }

    private void HandleSelectedItemChanged(InventoryItemId _)
    {
        RefreshPanel();
    }

    private void HandleItemClicked(InventoryItemId itemId)
    {
        focusedItem = itemId;
        RefreshDetails();
        RefreshEntryStates();
    }

    private void HandleActionButtonClicked()
    {
        if (inventorySystem == null || focusedItem == InventoryItemId.None) return;

        InventoryItemId selectedItem = inventoryModel != null
            ? inventoryModel.SelectedItem.Value
            : InventoryItemId.None;

        if (selectedItem == focusedItem)
        {
            inventorySystem.SelectItem(InventoryItemId.None);
        }
        else
        {
            inventorySystem.SelectItem(focusedItem);
        }
    }

    private void RefreshPanel()
    {
        RefreshFocusedItem();
        RebuildList();
        RefreshDetails();
        RefreshEntryStates();

        if (IsPanelOpen && !HasAnyItem())
        {
            ClosePanel();
        }
    }

    private void RefreshFocusedItem()
    {
        if (inventorySystem == null)
        {
            focusedItem = InventoryItemId.None;
            return;
        }

        if (focusedItem != InventoryItemId.None && inventorySystem.HasItem(focusedItem))
        {
            return;
        }

        InventoryItemId selectedItem = inventoryModel != null
            ? inventoryModel.SelectedItem.Value
            : InventoryItemId.None;

        if (selectedItem != InventoryItemId.None && inventorySystem.HasItem(selectedItem))
        {
            focusedItem = selectedItem;
            return;
        }

        IReadOnlyList<InventoryItemId> items = inventorySystem.GetItems();
        focusedItem = items != null && items.Count > 0 ? items[0] : InventoryItemId.None;
    }

    private void RebuildList()
    {
        ClearEntries();

        if (itemListRoot == null || itemEntryPrefab == null || inventorySystem == null) return;

        IReadOnlyList<InventoryItemId> items = inventorySystem.GetItems();
        if (items == null) return;

        for (int i = 0; i < items.Count; i++)
        {
            InventoryItemId itemId = items[i];
            InventoryItemEntryUI entry = Instantiate(itemEntryPrefab, itemListRoot);
            entry.Setup(itemId, GetDisplayName(itemId), HandleItemClicked);
            activeEntries.Add(entry);
        }
    }

    private void ClearEntries()
    {
        for (int i = 0; i < activeEntries.Count; i++)
        {
            if (activeEntries[i] != null)
            {
                Destroy(activeEntries[i].gameObject);
            }
        }

        activeEntries.Clear();
    }

    private void RefreshEntryStates()
    {
        InventoryItemId selectedItem = inventoryModel != null
            ? inventoryModel.SelectedItem.Value
            : InventoryItemId.None;

        for (int i = 0; i < activeEntries.Count; i++)
        {
            InventoryItemEntryUI entry = activeEntries[i];
            if (entry == null) continue;

            IReadOnlyList<InventoryItemId> items = inventorySystem != null ? inventorySystem.GetItems() : null;
            if (items == null || i >= items.Count) continue;

            InventoryItemId itemId = items[i];
            entry.SetState(itemId == focusedItem, itemId == selectedItem);
        }
    }

    private void RefreshDetails()
    {
        InventoryItemId selectedItem = inventoryModel != null
            ? inventoryModel.SelectedItem.Value
            : InventoryItemId.None;

        if (selectedItemTitleText != null)
        {
            selectedItemTitleText.text = focusedItem == InventoryItemId.None
                ? "No Item"
                : GetDisplayName(focusedItem);
        }

        if (selectedItemDescriptionText != null)
        {
            selectedItemDescriptionText.text = focusedItem == InventoryItemId.None
                ? "Inventory is empty."
                : GetDescription(focusedItem);
        }

        if (actionButton != null)
        {
            bool hasFocusedItem = focusedItem != InventoryItemId.None;
            actionButton.interactable = hasFocusedItem;
        }

        if (actionButtonLabelText != null)
        {
            if (focusedItem == InventoryItemId.None)
            {
                actionButtonLabelText.text = "No Item";
            }
            else if (selectedItem == focusedItem)
            {
                actionButtonLabelText.text = "Store";
            }
            else
            {
                actionButtonLabelText.text = "Take Out";
            }
        }
    }

    private bool HasAnyItem()
    {
        IReadOnlyList<InventoryItemId> items = inventorySystem != null ? inventorySystem.GetItems() : null;
        return items != null && items.Count > 0;
    }

    private string GetDisplayName(InventoryItemId itemId)
    {
        switch (itemId)
        {
            case InventoryItemId.CommunicationDevice:
                return "Communication Device";
            case InventoryItemId.SmallAxe:
                return "Small Axe";
            case InventoryItemId.Chainsaw:
                return "Chainsaw";
            default:
                return "Unknown Item";
        }
    }

    private string GetDescription(InventoryItemId itemId)
    {
        switch (itemId)
        {
            case InventoryItemId.CommunicationDevice:
                return "A portable HQ transmission device. You can review current mission details from it.";
            case InventoryItemId.SmallAxe:
                return "Useful for clearing smaller fallen branches and light debris.";
            case InventoryItemId.Chainsaw:
                return "Strong enough to cut through heavy tree trunks blocking the route.";
            default:
                return string.Empty;
        }
    }

    private void ResolveInteractionController()
    {
        if (playerInteractionController != null || playerController == null) return;
        playerInteractionController = playerController.GetComponent<PlayerInteractionController>();
    }

    protected override void ResolvePanelReferences()
    {
        ResolveInteractionController();
    }

    protected override bool TryOpenFromShortcut()
    {
        return OpenPanel();
    }
}
