using System.Collections;
using TMPro;
using UnityEngine;

public class DayTransitionPanel : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TMP_Text dayText;
    [SerializeField] private float fadeDuration = 1f;
    [SerializeField] private float holdDuration = 1.2f;

    private void Awake()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }
    }

    public IEnumerator PlayTransition(int dayNumber)
    {
        if (canvasGroup == null) yield break;

        if (dayText != null)
            dayText.text = $"Day {dayNumber}";

        yield return Fade(0f, 1f, fadeDuration);
        yield return new WaitForSeconds(holdDuration);
        yield return Fade(1f, 0f, fadeDuration);
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        float time = 0f;
        canvasGroup.alpha = from;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / duration);
            canvasGroup.alpha = Mathf.Lerp(from, to, t);
            yield return null;
        }

        canvasGroup.alpha = to;
    }
}