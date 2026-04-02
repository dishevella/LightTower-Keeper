using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HQTransmissionPanel : MonoBehaviour
{
    [SerializeField] private GameObject rootObject;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text messageContentText;
    [SerializeField] private Button closeButton;
    [SerializeField] private PlayerController playerController;

    public event Action Closed;

    private bool isOpen;

    private void Awake()
    {
        if (rootObject != null)
            rootObject.SetActive(false);

        if (closeButton != null)
            closeButton.onClick.AddListener(ClosePanel);
    }

    public void OpenMessage(string title, string message)
    {
        if (rootObject != null)
            rootObject.SetActive(true);

        if (titleText != null)
            titleText.text = title;

        if (messageContentText != null)
            messageContentText.text = message;

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
        if (!isOpen) return;

        if (rootObject != null)
            rootObject.SetActive(false);

        isOpen = false;

        if (playerController != null)
        {
            playerController.SetCanMove(true);
            playerController.SetCanLook(true);
            playerController.SetCursorLocked(true);
        }

        Closed?.Invoke();
    }

    public bool IsOpen()
    {
        return isOpen;
    }
}
