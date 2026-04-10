using System;
using UnityEngine;

[Serializable]
public class InteractableMessageSubtitle
{
    [SerializeField] private MessageSubtitlePanel panel;
    [TextArea(2, 4)]
    [SerializeField] private string line;
    [SerializeField] private float fadeIn = 0.15f;
    [SerializeField] private float hold = 1.8f;
    [SerializeField] private float fadeOut = 0.2f;

    public bool IsConfigured =>
        panel != null
        && !string.IsNullOrWhiteSpace(line);

    public void Play()
    {
        if (!IsConfigured) return;

        panel.PlayLineNow(line, fadeIn, hold, fadeOut);
    }
}
