using System.Collections;
using UnityEngine;

public class Day1DockEncounterController : ControllerAbstract
{
    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private Transform encounterPoint;
    [SerializeField] private float triggerDistance = 4f;
    [SerializeField] private ScreenSubtitlePanel screenSubtitlePanel;
    [SerializeField] private DockNpcSequenceController dockNpcSequenceController;
    [SerializeField] private MonoBehaviour[] playerControlComponents;

    [Header("Dialogue")]
    [TextArea(2, 4)]
    [SerializeField] private string owenWarningLine = "This island still isn't safe, and fishing isn't allowed here. You need to leave now.";
    [SerializeField] private float lineFadeIn = 0.35f;
    [SerializeField] private float lineHold = 2.4f;
    [SerializeField] private float lineFadeOut = 0.45f;

    private bool activePhase;
    private bool playing;

    private void Awake()
    {
        this.GetEvent().Register<Day1GoDockStartedEvent>(OnDay1GoDockStarted)
            .UnRegisterWhenGameObjectDestroyed(gameObject);
    }

    private void Update()
    {
        if (!activePhase || playing) return;
        if (player == null || encounterPoint == null) return;

        Vector3 playerPos = player.position;
        Vector3 targetPos = encounterPoint.position;
        playerPos.y = 0f;
        targetPos.y = 0f;

        if (Vector3.Distance(playerPos, targetPos) <= triggerDistance)
        {
            StartCoroutine(PlayDockEncounterRoutine());
        }
    }

    private void OnDay1GoDockStarted(Day1GoDockStartedEvent evt)
    {
        activePhase = true;
        playing = false;
    }

    private IEnumerator PlayDockEncounterRoutine()
    {
        activePhase = false;
        playing = true;
        SetPlayerLocked(true);

        if (screenSubtitlePanel != null)
        {
            yield return screenSubtitlePanel.PlayLine(
                owenWarningLine,
                lineFadeIn,
                lineHold,
                lineFadeOut);
        }

        if (dockNpcSequenceController != null)
        {
            dockNpcSequenceController.PlaySequence();
            yield return new WaitUntil(() => !dockNpcSequenceController.IsPlaying);
        }

        SetPlayerLocked(false);
        this.SendCommand(new FinishDay1DockEncounterCommand());
        playing = false;
    }

    private void SetPlayerLocked(bool locked)
    {
        if (playerControlComponents == null) return;

        for (int i = 0; i < playerControlComponents.Length; i++)
        {
            if (playerControlComponents[i] != null)
            {
                playerControlComponents[i].enabled = !locked;
            }
        }
    }
}
