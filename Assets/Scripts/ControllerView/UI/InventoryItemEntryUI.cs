using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventoryItemEntryUI : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private TMP_Text labelText;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color focusedColor = new Color(0.8f, 0.9f, 1f, 1f);
    [SerializeField] private Color selectedColor = new Color(0.6f, 0.85f, 1f, 1f);

    private InventoryItemId itemId;
    private Action<InventoryItemId> onClicked;

    private void Awake()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(HandleClick);
            button.onClick.AddListener(HandleClick);
        }
    }

    public void Setup(InventoryItemId newItemId, string label, Action<InventoryItemId> clickedCallback)
    {
        itemId = newItemId;
        onClicked = clickedCallback;

        if (labelText != null)
        {
            labelText.text = label;
        }
    }

    public void SetState(bool focused, bool selected)
    {
        if (backgroundImage == null) return;

        if (selected)
        {
            backgroundImage.color = selectedColor;
            return;
        }

        backgroundImage.color = focused ? focusedColor : normalColor;
    }

    private void HandleClick()
    {
        onClicked?.Invoke(itemId);
    }
}
