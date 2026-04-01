using System.Collections;
using TMPro;
using UnityEngine;

public class ScreenSubtitlePanel : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TextMeshProUGUI subtitleText;

    public void HideImmediate()
    {
        canvasGroup.alpha = 0f;
        subtitleText.text = string.Empty;
    }

    public IEnumerator PlayLine(string line, float fadeIn, float hold, float fadeOut)
    {
        subtitleText.text = line;

        float time = 0f;
        while (time < fadeIn)
        {
            time += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, time / fadeIn);
            yield return null;
        }

        canvasGroup.alpha = 1f;
        yield return new WaitForSeconds(hold);

        time = 0f;
        while (time < fadeOut)
        {
            time += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, time / fadeOut);
            yield return null;
        }

        canvasGroup.alpha = 0f;
    }
}