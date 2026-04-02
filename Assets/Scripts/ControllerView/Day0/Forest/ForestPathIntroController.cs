using System.Collections;
using UnityEngine;
using System;

public class ForestSubtitleTriggerController : ControllerAbstract
{
    [Serializable]
    public class SubtitleStep
    {
        public Transform triggerPoint;
        public float triggerDistance = 3f;
        public WorldSubtitleView view;
        public bool isLastStep;
        [HideInInspector] public bool triggered;
    }

    [Header("References")]
    [SerializeField] private Transform player;

    [SerializeField] private FadePanel fadePanel;
    [SerializeField] private ScreenSubtitlePanel screenSubtitlePanel;

    [SerializeField] private GameObject forestRoot;

    [SerializeField] private Transform lighthouseSpawnPoint;

    [Header("Ending Black Screen Line")]
    [TextArea(2, 4)]
    [SerializeField] private string endingBlackScreenLine = "The path opens";

    [Header("Timing")]
    [SerializeField] private float blackLineFadeIn = 0.4f;
    [SerializeField] private float blackLineHold = 1.8f;
    [SerializeField] private float blackLineFadeOut = 0.5f;
    [SerializeField] private float sceneFadeOutDuration = 1.5f;
    [SerializeField] private float sceneFadeInDuration = 1.5f;

    [Header("Lock Player")]
    [SerializeField] private MonoBehaviour[] playerControlComponents;

    [Header("Subtitle Steps")]
    [SerializeField] private SubtitleStep[] steps;

    private bool activePhase;
    private bool ending;
    
    private void Start()
    {
        this.GetEvent().Register<ForestPathStartedEvent>(OnForestPathStarted)
            .UnRegisterWhenGameObjectDestroyed(gameObject);
    }
    private void Awake()
    {
        
        if (steps != null)
        {
            foreach (var step in steps)
            {
                step.triggered = false;

                if (step.view != null)
                {
                    step.view.ResetView();
                }
            }
        }
    }

    private void Update()
    {
        
        if (!activePhase || ending) return;
        if (player == null || steps == null) return;

        Vector3 playerPos = player.position;
        playerPos.y = 0f;

        for (int i = 0; i < steps.Length; i++)
        {
            var step = steps[i];

            if (step.triggered) continue;
            if (step.triggerPoint == null) continue;
            if (step.view == null) continue;

            Vector3 pointPos = step.triggerPoint.position;
            pointPos.y = 0f;

            float distance = Vector3.Distance(playerPos, pointPos);

            if (distance <= step.triggerDistance)
            {
                step.triggered = true;
                if(step.isLastStep)
                {
                    step.view.OnFinished += HandleLastSubtitleFinished;
                }
                step.view.Play();
                //step.view.OnFinished -= HandleLastSubtitleFinished;
                Debug.Log($"Forest subtitle triggered: {i} at {step.triggerPoint.name}");
            }
        }
    }

    private void OnForestPathStarted(ForestPathStartedEvent evt)
    {
        activePhase = true;
        ending = false;
        
    }
       
    private IEnumerator EndForestAndEnterLighthouseApproach()
    {
        ending = true;
        SetPlayerLocked(true);
        yield return fadePanel.FadeToBlack(sceneFadeOutDuration);

        yield return screenSubtitlePanel.PlayLine(
            endingBlackScreenLine,
            blackLineFadeIn,
            blackLineHold,
            blackLineFadeOut
            );
        SwitchToLighthouseApproach();
        yield return new WaitForSeconds(0.2f);
        yield return fadePanel.FadeFromBlack(sceneFadeInDuration);
        SetPlayerLocked(false);
        activePhase = false;
    }
    private void SwitchToLighthouseApproach()
    {
        if (forestRoot != null)
            forestRoot.SetActive(false);
        if(player != null && lighthouseSpawnPoint!= null)
        {
            player.position = lighthouseSpawnPoint.position;
            player.rotation = lighthouseSpawnPoint.rotation;
        }
        this.SendCommand(new FinishForestPathCommand());
    }
    private void SetPlayerLocked(bool locked)
    {
        if (playerControlComponents == null) return;
        foreach(var comp in playerControlComponents)
        {
            if(comp!= null)
            {
                comp.enabled = !locked;
            }
        }
    }
    private void HandleLastSubtitleFinished()
    {
        if (!activePhase || ending) return;
        StartCoroutine(EndForestAndEnterLighthouseApproach());
    }
}    
