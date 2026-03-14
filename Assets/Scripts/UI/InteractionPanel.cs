using UnityEngine;
using TMPro;

public class InteractionHintPanel : MonoBehaviour
{
    [SerializeField] private GameObject rootObject;
    [SerializeField] private TMP_Text hintText;

    private void Awake()
    {
        Hide();
    }
    public void Show(string Message)
    {
        if (rootObject != null)
            rootObject.SetActive(true);
        if (hintText != null)
            hintText.text = Message;
    }
    public void Hide()
    {
        if (rootObject != null)
            rootObject.SetActive(false);
    }
}
