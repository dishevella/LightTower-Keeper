using UnityEngine;

public class Day1ReturnRouteController : ControllerAbstract
{
    private const string Task_ReturnToLighthouse = "task_return_lighthouse";

    [SerializeField] private Transform player;
    [SerializeField] private Transform lighthouseReturnPoint;
    [SerializeField] private float returnDistance = 4f;
    [SerializeField] private GameObject intactBridgeRoot;
    [SerializeField] private GameObject brokenBridgeRoot;
    [SerializeField] private GameObject[] objectsToEnableOnReturnRoute;
    [SerializeField] private GameObject[] objectsToDisableOnReturnRoute;

    private bool activePhase;
    private bool completed;

    private void Awake()
    {
        this.GetEvent().Register<Day1ReturnRouteStartedEvent>(OnDay1ReturnRouteStarted)
            .UnRegisterWhenGameObjectDestroyed(gameObject);
    }

    private void Update()
    {
        if (!activePhase || completed) return;
        if (player == null || lighthouseReturnPoint == null) return;

        Vector3 playerPos = player.position;
        Vector3 targetPos = lighthouseReturnPoint.position;
        playerPos.y = 0f;
        targetPos.y = 0f;

        if (Vector3.Distance(playerPos, targetPos) <= returnDistance)
        {
            completed = true;
            activePhase = false;
            this.SendCommand(new FinishDay1ReturnRouteCommand());
        }
    }

    private void OnDay1ReturnRouteStarted(Day1ReturnRouteStartedEvent evt)
    {
        activePhase = true;
        completed = false;

        if (intactBridgeRoot != null)
            intactBridgeRoot.SetActive(false);

        if (brokenBridgeRoot != null)
            brokenBridgeRoot.SetActive(true);

        SetObjectsActive(objectsToEnableOnReturnRoute, true);
        SetObjectsActive(objectsToDisableOnReturnRoute, false);

        var taskSystem = this.GetSystem<TaskSystem>();
        if (taskSystem != null)
        {
            taskSystem.SetTask(
                Task_ReturnToLighthouse,
                "Return to the Lighthouse",
                "Find the only safe route back to the lighthouse before dark.");
        }
    }

    private void SetObjectsActive(GameObject[] objects, bool active)
    {
        if (objects == null) return;

        for (int i = 0; i < objects.Length; i++)
        {
            if (objects[i] != null)
                objects[i].SetActive(active);
        }
    }
}
