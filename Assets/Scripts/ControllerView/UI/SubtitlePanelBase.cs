using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public abstract class SubtitlePanelBase : MonoBehaviour
{
    private int playToken;
    private Coroutine activeRoutine;

    protected abstract CanvasGroup CanvasGroup { get; }
    protected abstract TextMeshProUGUI SubtitleText { get; }
    protected virtual GameObject RootObject => null;
    protected virtual bool ToggleRootObjectOnVisibility => false;

    protected virtual void Awake()
    {
        ConfigureNonBlocking();
        HideImmediate();
    }

    public virtual void HideImmediate()
    {
        if (ToggleRootObjectOnVisibility && RootObject != null)
        {
            RootObject.SetActive(false);
        }

        if (CanvasGroup != null)
        {
            CanvasGroup.alpha = 0f;
            CanvasGroup.blocksRaycasts = false;
            CanvasGroup.interactable = false;
        }

        if (SubtitleText != null)
        {
            SubtitleText.text = string.Empty;
            Color color = SubtitleText.color;
            color.a = 0f;
            SubtitleText.color = color;
        }
    }

    public virtual IEnumerator PlayLine(string line, float fadeIn, float hold, float fadeOut)
    {
        int currentToken = ++playToken;

        if (SubtitleText == null || CanvasGroup == null)
        {
            yield break;
        }

        if (ToggleRootObjectOnVisibility && RootObject != null)
        {
            RootObject.SetActive(true);
        }

        SubtitleText.text = line;
        Color textColor = SubtitleText.color;
        textColor.a = 1f;
        SubtitleText.color = textColor;

        yield return FadeAlpha(currentToken, 0f, 1f, fadeIn);

        if (currentToken != playToken)
        {
            yield break;
        }

        CanvasGroup.alpha = 1f;

        if (hold > 0f)
        {
            yield return new WaitForSeconds(hold);
        }

        if (currentToken != playToken)
        {
            yield break;
        }

        yield return FadeAlpha(currentToken, 1f, 0f, fadeOut);

        if (currentToken != playToken)
        {
            yield break;
        }

        HideImmediate();
    }

    public virtual void PlayLineNow(string line, float fadeIn, float hold, float fadeOut)
    {
        if (ToggleRootObjectOnVisibility && RootObject != null)
        {
            RootObject.SetActive(true);
        }

        if (activeRoutine != null)
        {
            StopCoroutine(activeRoutine);
            activeRoutine = null;
        }

        activeRoutine = StartCoroutine(PlayLineRoutine(line, fadeIn, hold, fadeOut));
    }

    protected virtual void ConfigureNonBlocking()
    {
        if (CanvasGroup != null)
        {
            CanvasGroup.blocksRaycasts = false;
            CanvasGroup.interactable = false;
        }

        Graphic[] graphics = GetComponentsInChildren<Graphic>(true);
        for (int i = 0; i < graphics.Length; i++)
        {
            graphics[i].raycastTarget = false;
        }
    }

    private IEnumerator PlayLineRoutine(string line, float fadeIn, float hold, float fadeOut)
    {
        yield return PlayLine(line, fadeIn, hold, fadeOut);
        activeRoutine = null;
    }

    private IEnumerator FadeAlpha(int token, float from, float to, float duration)
    {
        if (duration <= 0f)
        {
            CanvasGroup.alpha = to;
            yield break;
        }

        float time = 0f;
        CanvasGroup.alpha = from;

        while (time < duration)
        {
            if (token != playToken)
            {
                yield break;
            }

            time += Time.deltaTime;
            CanvasGroup.alpha = Mathf.Lerp(from, to, time / duration);
            yield return null;
        }

        CanvasGroup.alpha = to;
    }
}

public static class SubtitleTimingUtility
{
    private const float ReadingLeadIn = 0.65f;
    private const float CharactersPerSecond = 18f;
    private const float MinorPauseSeconds = 0.08f;
    private const float MajorPauseSeconds = 0.16f;
    private const float LongPauseSeconds = 0.24f;
    private const float MaximumAdditionalHold = 2.75f;

    public static void ResolveTimings(
        string text,
        float configuredFadeIn,
        float configuredHold,
        float configuredFadeOut,
        float defaultFadeIn,
        float defaultHold,
        float defaultFadeOut,
        out float fadeIn,
        out float hold,
        out float fadeOut)
    {
        fadeIn = configuredFadeIn > 0f ? configuredFadeIn : defaultFadeIn;
        fadeOut = configuredFadeOut > 0f ? configuredFadeOut : defaultFadeOut;
        hold = configuredHold > 0f
            ? configuredHold
            : CalculateAutomaticHold(text, defaultHold);
    }

    public static float CalculateAutomaticHold(string text, float minimumHold)
    {
        float resolvedMinimumHold = Mathf.Max(0.1f, minimumHold);

        if (string.IsNullOrWhiteSpace(text))
        {
            return resolvedMinimumHold;
        }

        int readableCharacterCount = 0;
        float punctuationPause = 0f;

        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            if (char.IsWhiteSpace(c))
            {
                continue;
            }

            switch (c)
            {
                case ',':
                case ';':
                case ':':
                case '，':
                case '、':
                case '；':
                case '：':
                    punctuationPause += MinorPauseSeconds;
                    continue;

                case '.':
                case '!':
                case '?':
                case '。':
                case '！':
                case '？':
                    punctuationPause += MajorPauseSeconds;
                    continue;

                case '…':
                    punctuationPause += LongPauseSeconds;
                    continue;
            }

            readableCharacterCount++;
        }

        float readingTime = ReadingLeadIn + (readableCharacterCount / CharactersPerSecond) + punctuationPause;
        float maximumHold = resolvedMinimumHold + MaximumAdditionalHold;

        return Mathf.Clamp(Mathf.Max(resolvedMinimumHold, readingTime), resolvedMinimumHold, maximumHold);
    }
}
