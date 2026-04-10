using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using System;

public class CinematicLetterboxPanel : MonoBehaviour
{
    public static event Action<bool, float> VisibilityChanged;
    public static bool IsAnyLetterboxVisible { get; private set; }

    [SerializeField] private GameObject rootObject;
    [SerializeField] private RectTransform topBar;
    [SerializeField] private RectTransform bottomBar;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private float shownBarHeight = 120f;

    private Coroutine activeRoutine;
    private float currentBarHeight;
    private float currentAlpha;

    private void Awake()
    {
        ConfigureNonBlocking();
        SetHiddenImmediate();
    }

    public void SetShownImmediate()
    {
        if (rootObject != null)
        {
            rootObject.SetActive(true);
        }

        NotifyVisibilityChanged(true, 0f);
        SetAlpha(1f);
        SetBarHeight(shownBarHeight);
    }

    public void SetHiddenImmediate()
    {
        StopActiveRoutine();
        NotifyVisibilityChanged(false, 0f);
        SetAlpha(0f);
        SetBarHeight(0f);

        if (rootObject != null)
        {
            rootObject.SetActive(false);
        }
    }

    public IEnumerator Show(float duration)
    {
        StopActiveRoutine();
        NotifyVisibilityChanged(true, duration);

        if (rootObject != null)
        {
            rootObject.SetActive(true);
        }

        yield return Animate(currentBarHeight, shownBarHeight, currentAlpha, 1f, duration);
    }

    public IEnumerator Hide(float duration)
    {
        StopActiveRoutine();
        NotifyVisibilityChanged(false, duration);
        yield return Animate(currentBarHeight, 0f, currentAlpha, 0f, duration);

        if (rootObject != null)
        {
            rootObject.SetActive(false);
        }
    }

    public void ShowNow(float duration)
    {
        StopActiveRoutine();
        activeRoutine = StartCoroutine(ShowRoutine(duration));
    }

    public void HideNow(float duration)
    {
        StopActiveRoutine();
        activeRoutine = StartCoroutine(HideRoutine(duration));
    }

    private IEnumerator ShowRoutine(float duration)
    {
        yield return Show(duration);
        activeRoutine = null;
    }

    private IEnumerator HideRoutine(float duration)
    {
        yield return Hide(duration);
        activeRoutine = null;
    }

    private IEnumerator Animate(float fromHeight, float toHeight, float fromAlpha, float toAlpha, float duration)
    {
        if (duration <= 0f)
        {
            SetBarHeight(toHeight);
            SetAlpha(toAlpha);
            yield break;
        }

        float time = 0f;
        SetBarHeight(fromHeight);
        SetAlpha(fromAlpha);

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;
            SetBarHeight(Mathf.Lerp(fromHeight, toHeight, t));
            SetAlpha(Mathf.Lerp(fromAlpha, toAlpha, t));
            yield return null;
        }

        SetBarHeight(toHeight);
        SetAlpha(toAlpha);
    }

    private void SetBarHeight(float height)
    {
        currentBarHeight = Mathf.Max(0f, height);

        if (topBar != null)
        {
            Vector2 size = topBar.sizeDelta;
            size.y = currentBarHeight;
            topBar.sizeDelta = size;
        }

        if (bottomBar != null)
        {
            Vector2 size = bottomBar.sizeDelta;
            size.y = currentBarHeight;
            bottomBar.sizeDelta = size;
        }
    }

    private void SetAlpha(float alpha)
    {
        currentAlpha = Mathf.Clamp01(alpha);

        if (canvasGroup != null)
        {
            canvasGroup.alpha = currentAlpha;
        }
    }

    private void NotifyVisibilityChanged(bool visible, float duration)
    {
        IsAnyLetterboxVisible = visible;
        VisibilityChanged?.Invoke(visible, duration);
    }

    private void ConfigureNonBlocking()
    {
        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }

        Graphic[] graphics = GetComponentsInChildren<Graphic>(true);
        for (int i = 0; i < graphics.Length; i++)
        {
            graphics[i].raycastTarget = false;
        }
    }

    private void StopActiveRoutine()
    {
        if (activeRoutine == null) return;

        StopCoroutine(activeRoutine);
        activeRoutine = null;
    }
}
