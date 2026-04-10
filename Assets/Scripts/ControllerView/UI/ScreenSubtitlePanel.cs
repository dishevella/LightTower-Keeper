using TMPro;
using UnityEngine;

public class ScreenSubtitlePanel : SubtitlePanelBase
{
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TextMeshProUGUI subtitleText;
    protected override CanvasGroup CanvasGroup => canvasGroup;
    protected override TextMeshProUGUI SubtitleText => subtitleText;
}
