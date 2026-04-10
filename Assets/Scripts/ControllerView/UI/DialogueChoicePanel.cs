using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DialogueChoicePanel : ControllerAbstract
{
    public enum DialogueTone
    {
        Harsh = 0,
        Neutral = 1,
        Gentle = 2
    }

    [Serializable]
    public class ChoiceButtonBinding
    {
        public Button button;
        public TextMeshProUGUI labelText;
    }

    [SerializeField] private GameObject rootObject;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private ChoiceButtonBinding harshChoice;
    [SerializeField] private ChoiceButtonBinding neutralChoice;
    [SerializeField] private ChoiceButtonBinding gentleChoice;

    private Action<DialogueTone> onChoiceSelected;

    public bool IsShowing => rootObject != null && rootObject.activeSelf;

    private void Awake()
    {
        HideImmediate();
        ConfigureCanvasGroup();
    }

    public void ShowChoices(
        string harshLine,
        string neutralLine,
        string gentleLine,
        Action<DialogueTone> onChoice)
    {
        onChoiceSelected = onChoice;

        ConfigureChoice(harshChoice, harshLine, DialogueTone.Harsh);
        ConfigureChoice(neutralChoice, neutralLine, DialogueTone.Neutral);
        ConfigureChoice(gentleChoice, gentleLine, DialogueTone.Gentle);

        if (rootObject != null)
        {
            rootObject.SetActive(true);
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        if (harshChoice != null && harshChoice.button != null)
        {
            EventSystem.current?.SetSelectedGameObject(harshChoice.button.gameObject);
        }
    }

    public void HideImmediate()
    {
        ClearChoiceListeners();

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        if (rootObject != null)
        {
            rootObject.SetActive(false);
        }
    }

    private void ConfigureCanvasGroup()
    {
        if (canvasGroup == null) return;

        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    private void ConfigureChoice(ChoiceButtonBinding binding, string label, DialogueTone tone)
    {
        if (binding == null) return;

        if (binding.labelText != null)
        {
            binding.labelText.text = label;
        }

        if (binding.button == null) return;

        binding.button.onClick.RemoveAllListeners();
        binding.button.onClick.AddListener(() => HandleChoiceSelected(tone));
    }

    private void ClearChoiceListeners()
    {
        ClearChoiceListener(harshChoice);
        ClearChoiceListener(neutralChoice);
        ClearChoiceListener(gentleChoice);
    }

    private void ClearChoiceListener(ChoiceButtonBinding binding)
    {
        if (binding == null || binding.button == null) return;

        binding.button.onClick.RemoveAllListeners();
    }

    private void HandleChoiceSelected(DialogueTone tone)
    {
        Action<DialogueTone> callback = onChoiceSelected;
        onChoiceSelected = null;

        HideImmediate();
        callback?.Invoke(tone);
    }
}
