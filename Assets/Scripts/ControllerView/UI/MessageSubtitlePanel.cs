using TMPro;
using UnityEngine;
using System.Collections;

public class MessageSubtitlePanel : SubtitlePanelBase
{
    private static int visiblePanelCount;

    [SerializeField] private GameObject rootObject;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TextMeshProUGUI subtitleText;
    [Header("Layout")]
    [SerializeField] private RectTransform layoutTarget;
    [SerializeField] private float cinematicOffsetY = 120f;

    private bool countsAsVisible;
    private Vector2 defaultAnchoredPosition;
    private Coroutine layoutRoutine;

    public static bool HasVisibleMessageSubtitle => visiblePanelCount > 0;

    protected override CanvasGroup CanvasGroup => canvasGroup;
    protected override TextMeshProUGUI SubtitleText => subtitleText;
    protected override GameObject RootObject => rootObject;
    protected override bool ToggleRootObjectOnVisibility => true;

    protected override void Awake()
    {
        if (layoutTarget == null && rootObject != null)
        {
            layoutTarget = rootObject.GetComponent<RectTransform>();
        }

        if (layoutTarget != null)
        {
            defaultAnchoredPosition = layoutTarget.anchoredPosition;
        }

        CinematicLetterboxPanel.VisibilityChanged += HandleLetterboxVisibilityChanged;
        base.Awake();
        ApplyLayoutImmediate(CinematicLetterboxPanel.IsAnyLetterboxVisible);
    }

    private void OnDestroy()
    {
        CinematicLetterboxPanel.VisibilityChanged -= HandleLetterboxVisibilityChanged;

        if (layoutRoutine != null)
        {
            StopCoroutine(layoutRoutine);
            layoutRoutine = null;
        }
    }

    public override void HideImmediate()
    {
        SetVisibleState(false);
        base.HideImmediate();
    }

    public override IEnumerator PlayLine(string line, float fadeIn, float hold, float fadeOut)
    {
        SetVisibleState(true);
        yield return base.PlayLine(line, fadeIn, hold, fadeOut);
    }

    private void HandleLetterboxVisibilityChanged(bool visible, float duration)
    {
        if (layoutTarget == null) return;

        if (layoutRoutine != null)
        {
            StopCoroutine(layoutRoutine);
            layoutRoutine = null;
        }

        if (!gameObject.activeInHierarchy || !isActiveAndEnabled || duration <= 0f)
        {
            ApplyLayoutImmediate(visible);
            return;
        }

        layoutRoutine = StartCoroutine(AnimateLayoutRoutine(visible, duration));
    }

    private IEnumerator AnimateLayoutRoutine(bool cinematicVisible, float duration)
    {
        Vector2 from = layoutTarget.anchoredPosition;
        Vector2 to = GetTargetLayoutPosition(cinematicVisible);

        float time = 0f;
        while (time < duration)
        {
            time += Time.deltaTime;
            layoutTarget.anchoredPosition = Vector2.Lerp(from, to, time / duration);
            yield return null;
        }

        layoutTarget.anchoredPosition = to;
        layoutRoutine = null;
    }

    private void ApplyLayoutImmediate(bool cinematicVisible)
    {
        if (layoutTarget == null) return;

        layoutTarget.anchoredPosition = GetTargetLayoutPosition(cinematicVisible);
    }

    private Vector2 GetTargetLayoutPosition(bool cinematicVisible)
    {
        return cinematicVisible
            ? defaultAnchoredPosition + Vector2.up * cinematicOffsetY
            : defaultAnchoredPosition;
    }

    private void SetVisibleState(bool visible)
    {
        if (visible == countsAsVisible) return;

        countsAsVisible = visible;
        visiblePanelCount += visible ? 1 : -1;

        if (visiblePanelCount < 0)
        {
            visiblePanelCount = 0;
        }
    }
}
