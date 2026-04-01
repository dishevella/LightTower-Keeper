using UnityEngine;
using System.Collections;

public class FadePanel : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;
    public void SetBlackImmediate()
    {
        canvasGroup.alpha = 1f;
        gameObject.SetActive(true);
    }
    public void SetClearImmediate()
    {
        canvasGroup.alpha = 0f;
        gameObject.SetActive(true);
    }
  
    public IEnumerator FadeFromBlack(float duration)
    {
        gameObject.SetActive(true);

        float time = 0f;
        while (time < duration)
        {
            time += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, time / duration);
            yield return null;
        }

        canvasGroup.alpha = 0f;
    }
    public IEnumerator FadeToBlack(float duration)
    {
        gameObject.SetActive(true);

        float time = 0f;
        while (time < duration)
        {
            time += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, time / duration);
            yield return null;
        }

        canvasGroup.alpha = 1f;
    }
}
