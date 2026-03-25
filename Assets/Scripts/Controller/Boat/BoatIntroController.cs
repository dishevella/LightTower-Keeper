using UnityEngine;
using System.Collections;
using Unity.Collections;

public class BoatIntroController : ControllerAbstract
{
    [System.Serializable]
    public class SubtitleStep
    {
        public Transform triggerPoint;
        public float triggerDistance = 2f;
        public WorldSubtitleView view;
        [HideInInspector] public bool triggered;
    }

    [Header("References")]
    [SerializeField] private FadePanel fadePanel;
    [SerializeField] private ScreenSubtitlePanel screenSubtitlePanel;
    [SerializeField] private BoatMover boatMover;
    [SerializeField] private GameObject boatIntroRoot;
    [SerializeField] private GameObject forestRoot;
    [SerializeField] private Transform player;
    [SerializeField] private Transform forestSpawnPoint;

    [Header("Black Screen")]
    [TextArea(2, 4)]
    [SerializeField] private string firstBlackScreenLine = "the voice is bigger than i thought";

    [Header("Timing")]
    [SerializeField] private float firstLineFadeIn = 0.4f;
    [SerializeField] private float firstLineHold = 1.8f;
    [SerializeField] private float firstLineFadeOut = 0.5f;
    [SerializeField] private float sceneFadeInDuration = 2f;
    [SerializeField] private float blackHoldBeforeReveal = 0.8f;
    [SerializeField] private float sceneFadeOutDuration = 2f;

    [Header("World Subtitle Steps")]
    [SerializeField] private SubtitleStep[] steps;
    private bool playing;

    [Header("Lock Movement and Look")]
    [SerializeField] private MonoBehaviour[] lockBeforeRevealComponent;

    private void OnEnable()
    {
        App.Events.Subscribe<BoatIntroStartedEvent>(OnBoatIntroStarted);
    }

    private void OnDisable()
    {
        App.Events.Unsubscribe<BoatIntroStartedEvent>(OnBoatIntroStarted);
    }

    private void Awake()
    {
        PrepareInitialState();
        SetPlayerControlLocked(true);
    }

    private void PrepareInitialState()
    {
        fadePanel.SetBlackImmediate();
        screenSubtitlePanel.HideImmediate();

        if (boatMover != null)
        {
            boatMover.ResetToStart();
        }   

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

    private void SetPlayerControlLocked(bool locked)
    {
        if (lockBeforeRevealComponent == null) return;
        foreach(var comp in lockBeforeRevealComponent)
        {
            if(comp != null)
            {
                comp.enabled = !locked;
            }
        }
    }
    private void OnBoatIntroStarted(BoatIntroStartedEvent evt)
    {
        if (playing) return;

        StartCoroutine(PlayIntroRoutine());
    }

    private IEnumerator PlayIntroRoutine()
    {
        playing = true;
        PrepareInitialState();

        yield return screenSubtitlePanel.PlayLine(
            firstBlackScreenLine,
            firstLineFadeIn,
            firstLineHold,
            firstLineFadeOut
        );

        yield return new WaitForSeconds(blackHoldBeforeReveal);

        yield return fadePanel.FadeFromBlack(sceneFadeInDuration);

        SetPlayerControlLocked(false);

        boatMover.Play();

        while (!boatMover.IsFinished)
        {
            
            if (steps!= null )
            {
                for(int i=0; i<steps.Length; i++)
                {
                    var step = steps[i];

                    if (step.triggered) continue;
                    if (step.triggerPoint == null) continue;
                    if (step.view == null) continue;
                    
                    Vector3 boatPos = boatMover.transform.position;
                    Vector3 pointPos = step.triggerPoint.position;
                    boatPos.y = 0f;
                    pointPos.y = 0f;
                    float distance = Vector3.Distance(boatPos,pointPos);
                    
                    if (distance <= step.triggerDistance)
                    {
                        step.triggered = true;
                        step.view.Play();

                        Debug.Log($"Trigger Subtitle{i} at point:{ step.triggerPoint.name}");
                    }
                }
            }

            yield return null;
        }


        yield return StartCoroutine(EndBoatIntroAndEnterForest());

        playing = false;

        if (boatIntroRoot != null)
            boatIntroRoot.SetActive(false);
    }
    private IEnumerator EndBoatIntroAndEnterForest()
    {
        SetPlayerControlLocked(true);
        yield return fadePanel.FadeToBlack(sceneFadeOutDuration);
        player.SetParent(null);
        SwitchToForestSection();
        yield return new WaitForSeconds(0.3f);
        yield return fadePanel.FadeFromBlack(sceneFadeInDuration);
        SetPlayerControlLocked(false);
    }
    private void SwitchToForestSection()
    {
     
        if (forestRoot != null)
            forestRoot.SetActive(true);
        if (player != null&&forestSpawnPoint!= null)
        {
            player.transform.position = forestSpawnPoint.position;
            player.transform.rotation = forestSpawnPoint.rotation;
        }
       App.Commands.Dispatch(new FinishBoatIntroCommand());

    }
}


