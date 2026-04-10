using System;
using UnityEngine;

public class InventoryHeldItemController : ControllerAbstract
{
    [Serializable]
    public class HeldItemBinding
    {
        public InventoryItemId itemId;
        public GameObject rootObject;
    }

    [Header("Held Objects")]
    [SerializeField] private HeldItemBinding[] bindings;

    [Header("Holding Animation")]
    [SerializeField] private Animator playerAnimator;
    [SerializeField] private string isHoldingBoolName = "IsHoldingItem";
    [SerializeField] private string holdTypeIntName = "HoldType";
    [SerializeField] private string holdingLayerName = "Holding";
    [SerializeField] private bool driveHoldingLayerWeight = true;
    [SerializeField][Range(0f, 1f)] private float holdingLayerWeight = 1f;
    [SerializeField] private float layerBlendSpeed = 10f;

    private InventoryModel inventoryModel;
    private int isHoldingBoolHash;
    private int holdTypeIntHash;
    private int holdingLayerIndex = -1;
    private float targetLayerWeight;

    private void Awake()
    {
        isHoldingBoolHash = Animator.StringToHash(isHoldingBoolName);
        holdTypeIntHash = Animator.StringToHash(holdTypeIntName);

        if (playerAnimator != null && driveHoldingLayerWeight && !string.IsNullOrEmpty(holdingLayerName))
        {
            holdingLayerIndex = playerAnimator.GetLayerIndex(holdingLayerName);
        }
    }

    private void Start()
    {
        inventoryModel = this.GetModel<InventoryModel>();
        if (inventoryModel == null) return;

        inventoryModel.SelectedItem.OnValueChanged += HandleSelectedItemChanged;
        ApplySelection(inventoryModel.SelectedItem.Value);
    }

    private void Update()
    {
        if (playerAnimator == null) return;
        if (!driveHoldingLayerWeight) return;
        if (holdingLayerIndex < 0) return;

        float currentWeight = playerAnimator.GetLayerWeight(holdingLayerIndex);
        float nextWeight = Mathf.Lerp(currentWeight, targetLayerWeight, layerBlendSpeed * Time.deltaTime);
        playerAnimator.SetLayerWeight(holdingLayerIndex, nextWeight);
    }

    private void OnDestroy()
    {
        if (inventoryModel == null) return;
        inventoryModel.SelectedItem.OnValueChanged -= HandleSelectedItemChanged;
    }

    private void HandleSelectedItemChanged(InventoryItemId itemId)
    {
        ApplySelection(itemId);
    }

    private void ApplySelection(InventoryItemId selectedItem)
    {
        ApplyHeldObjects(selectedItem);
        ApplyHoldingAnimation(selectedItem);
    }

    private void ApplyHeldObjects(InventoryItemId selectedItem)
    {
        if (bindings == null) return;

        for (int i = 0; i < bindings.Length; i++)
        {
            HeldItemBinding binding = bindings[i];
            if (binding == null || binding.rootObject == null) continue;

            binding.rootObject.SetActive(binding.itemId == selectedItem);
        }
    }

    private void ApplyHoldingAnimation(InventoryItemId selectedItem)
    {
        if (playerAnimator == null) return;

        bool isHoldingItem = selectedItem != InventoryItemId.None;
        int holdType = GetHoldTypeValue(selectedItem);

        if (!string.IsNullOrEmpty(isHoldingBoolName))
        {
            playerAnimator.SetBool(isHoldingBoolHash, isHoldingItem);
        }

        if (!string.IsNullOrEmpty(holdTypeIntName))
        {
            playerAnimator.SetInteger(holdTypeIntHash, holdType);
        }

        if (driveHoldingLayerWeight && holdingLayerIndex >= 0)
        {
            targetLayerWeight = isHoldingItem ? holdingLayerWeight : 0f;
        }
    }

    private int GetHoldTypeValue(InventoryItemId itemId)
    {
        switch (itemId)
        {
            case InventoryItemId.CommunicationDevice:
                return 1;

            case InventoryItemId.SmallAxe:
                return 2;

            case InventoryItemId.Chainsaw:
                return 3;

            default:
                return 0;
        }
    }
}
