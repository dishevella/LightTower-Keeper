using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class InteractionHintPanel : MonoBehaviour
{
    [SerializeField] private GameObject rootObject;
    [SerializeField] private TMP_Text hintText;

    private void Awake()
    {
        ConfigureNonBlocking();
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

    private void ConfigureNonBlocking()
    {
        Graphic[] graphics = GetComponentsInChildren<Graphic>(true);
        for (int i = 0; i < graphics.Length; i++)
        {
            graphics[i].raycastTarget = false;
        }
    }
}
