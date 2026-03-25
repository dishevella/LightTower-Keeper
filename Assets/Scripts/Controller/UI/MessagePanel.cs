using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MessagePanel : MonoBehaviour
{
    [SerializeField] private GameObject rootObject;
    [SerializeField] private TMP_Text messageContentText;
    [SerializeField] private Button sendButton;
    [SerializeField] private Button closeButton;

    [TextArea]
    [SerializeField] private string draftMessage = "HQ, this is Owen. I have arrived at the lighthouse safely.";

    [SerializeField] private PlayerController playerController;

    private bool isOpen;

    private void Awake()
    {
        if (rootObject != null)
            rootObject.SetActive(false);

        if (sendButton != null)
            sendButton.onClick.AddListener(OnClickSend);

        if (closeButton != null)
            closeButton.onClick.AddListener(ClosePanel);
    }

    public void OpenPanel()
    {
        if (rootObject != null)
            rootObject.SetActive(true);

        if (messageContentText != null)
            messageContentText.text = draftMessage;

        isOpen = true;

        if (playerController != null)
        {
            playerController.SetCanMove(false);
            playerController.SetCanLook(false);
            playerController.SetCursorLocked(false);
        }
    }

    public void ClosePanel()
    {
        if (rootObject != null)
            rootObject.SetActive(false);

        isOpen = false;

        if (playerController != null)
        {
            playerController.SetCanMove(true);
            playerController.SetCanLook(true);
            playerController.SetCursorLocked(true);
        }
    }

    private void OnClickSend()
    {
        

        ClosePanel();
    }

    public bool IsOpen()
    {
        return isOpen;
    }
}