using System.Collections;
using UnityEngine;
using System;

public class ApproachLighthouseController : ControllerAbstract
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
    private const string Task_EnterLightHouse = "task_enter_lighthouse";
    [Header("Reference")]
    [SerializeField] private Transform player;
    [SerializeField] private Transform playerCameraPivot;
    [SerializeField] private Transform lighthouseLookTarget;

    [Header("Look At Lighthouse")]
    [SerializeField] private float turnToLighthouseDuration = 1f;
    [SerializeField] private float lookDuration = 2f;
    [SerializeField] private float returnDuration = 1f;

    [Header("Playing the opening")]
    public bool opeingAnimation = true;

    [Header("Locked Components")]
    [SerializeField] private MonoBehaviour[] playerControlComponents;
    [SerializeField] private PlayerInteractionController playerInteractionController;
  
    [Header("Intro Subtitle")]
    [SerializeField] private WorldSubtitleView introSubtitleView;
    [SerializeField] private bool waitIntroSubtitleFinished = false;

    [Header("Subtitle Steps")]
    [SerializeField] private SubtitleStep[] steps;

    private bool playing;
    private bool ending;
    
    private Quaternion originalCameraRotation;
    private void Awake()
    {
        if (steps!=null)
        {
            foreach(var step in steps)
            {
                step.triggered = false;
                if(step.view != null)
                {
                    step.view.ResetView();
                }
            }
        }
        
        SetInteractionLocked(true);
    }
    private void Update()
    {
        if (ending||steps == null || player == null) return;
        Vector3 playerPos = player.position;
        playerPos.y = 0f;
        for(int i =0;i< steps.Length; i++)
        {
            var step = steps [i];
            if (step.triggered) continue;
            if (step.triggerPoint == null) continue;
            if (step.view == null) continue;
            Vector3 pointPos = step.triggerPoint.position;
            pointPos.y = 0f;
            float distance = Vector3.Distance(playerPos, pointPos);
            if(distance <= step.triggerDistance)
            {
                step.triggered = true;
                if(step.isLastStep)
                {
                    step.view.OnFinished -= HandleLastSubtitleFinished;
                    step.view.OnFinished += HandleLastSubtitleFinished;
                }
                step.view.Play();
                Debug.Log($"Lighthouse subtitle triggered:{i}");
            }
        }
    }
    private void Start()
    {

        this.GetEvent().Register<ApproachLighthouseStartedEvent>(OnApproachLighthouseStarted)
            .UnRegisterWhenGameObjectDestroyed(gameObject);
        this.SendCommand(new FinishForestPathCommand());
    }
    private void OnApproachLighthouseStarted(ApproachLighthouseStartedEvent evt)
    {
        if (playing) return;
        ending = false;
        if (opeingAnimation) 
        StartCoroutine(PlayRoutine());
    }
    private IEnumerator PlayRoutine()
    {
        playing = true;
        SetPlayerControlLocked(true);
        if(playerCameraPivot != null)
        {
            originalCameraRotation = playerCameraPivot.rotation;
        }
        if (playerCameraPivot != null && lighthouseLookTarget != null)
        {
            Quaternion startRot = playerCameraPivot.rotation;
            Vector3 dir = lighthouseLookTarget.position - playerCameraPivot.position;
            Quaternion targetRot = Quaternion.LookRotation(dir.normalized, Vector3.up);

            float t = 0f;
            while (t < turnToLighthouseDuration)
            {
                t += Time.deltaTime;
                playerCameraPivot.rotation = Quaternion.Slerp(startRot, targetRot, t / turnToLighthouseDuration);
                yield return null;
            }
            playerCameraPivot.rotation = targetRot;
        }
        yield return new WaitForSeconds(0.5f);
        if (introSubtitleView != null)
        {
            introSubtitleView.ResetView();
            introSubtitleView.Play();
            if(waitIntroSubtitleFinished)
            {
                bool finished = false;
                void OnFinished() => finished = true;
                introSubtitleView.OnFinished += OnFinished;
                yield return new WaitUntil(() => finished);
                introSubtitleView.OnFinished -= OnFinished;
            }
            else
            {
                yield return new WaitForSeconds(lookDuration);
            }
        }
        else
        {
            yield return new WaitForSeconds(lookDuration);
        }

        if (playerCameraPivot != null)
        {
            Quaternion startRot = playerCameraPivot.rotation;
            Quaternion endRot = originalCameraRotation;

            float t = 0f;
            while (t < returnDuration)
            {
                t += Time.deltaTime;
                playerCameraPivot.rotation = Quaternion.Slerp(startRot, endRot, t / returnDuration);
                yield return null;
            }
            playerCameraPivot.rotation = endRot;
        }
       
            SetPlayerControlLocked(false);
            playing = false;
    }
    private void SetPlayerControlLocked(bool locked)
    {
        if (playerControlComponents == null) return;
        foreach (var comp in playerControlComponents)
        {
            if (comp != null)
                comp.enabled = !locked;
        }
    }
    private void SetInteractionLocked(bool locked)
    {
        if (playerInteractionController != null)
        {
            playerInteractionController.SetInteractionEnabled(!locked);
        }
    }
    private void HandleLastSubtitleFinished()
    {
        if (ending) return;
        
        SetInteractionLocked(false);
        ending = true;
    }
}
