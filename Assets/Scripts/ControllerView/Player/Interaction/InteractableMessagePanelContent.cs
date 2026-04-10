using System;
using UnityEngine;

[Serializable]
public class InteractableMessagePanelContent
{
    [SerializeField] private MessageSubtitlePanel panel;
    [TextArea(2, 5)]
    [SerializeField] private string message;
    [SerializeField] private float fadeIn = 0.15f;
    [SerializeField] private float hold = 1.8f;
    [SerializeField] private float fadeOut = 0.2f;

    public bool IsConfigured =>
        panel != null
        && !string.IsNullOrWhiteSpace(message);

    public void Show()
    {
        if (!IsConfigured) return;

        panel.PlayLineNow(message, fadeIn, hold, fadeOut);
    }
}
