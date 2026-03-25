using UnityEngine;

public class BoatMover : MonoBehaviour
{
    [SerializeField] private Transform[] pathPoints;
    [SerializeField] private float moveSpeed = 2.5f;
    [SerializeField] private float rotationLerp = 4f;
    [SerializeField] private float bobAmount = 0.08f;
    [SerializeField] private float bobFrequency = 1.6f;

    //debug the progress of the route
    [SerializeField] private float debugNormalizedProgress;
    [SerializeField] private float debugTravelledDistance;
    [SerializeField] private float debugTotalDistance;
    [SerializeField] private int debugCurrentTargetIndex;
    [SerializeField] private bool debugIsPlaying;

    private int targetIndex;
    private bool playing;
    private float totalDistance;
    private float travelledDistance;
    private Vector3 trackPosition;

    public float NormalizedProgress
    {
        get
        {
            if (totalDistance <= 0.001f) return 1f;
            return Mathf.Clamp01(travelledDistance / totalDistance);
        }
    }

    public bool IsFinished => !playing && targetIndex >= pathPoints.Length;

    private void Awake()
    {
        CalculateTotalDistance();
        ResetToStart();
    }

    public void ResetToStart()
    {
        if (pathPoints == null || pathPoints.Length == 0) return;

        targetIndex = 1;
        travelledDistance = 0f;
        playing = false;
        trackPosition = pathPoints[0].position;
        transform.position = trackPosition;
    }

    public void Play()
    {
        if (pathPoints == null || pathPoints.Length < 2) return;

        targetIndex = 1;
        travelledDistance = 0f;
        playing = true;
        trackPosition = pathPoints[0].position;
        transform.position = trackPosition;
    }

    private void Update()
    {
        if (!playing) return;

        if (targetIndex >= pathPoints.Length)
        {
            playing = false;
            return;
        }

        Vector3 target = pathPoints[targetIndex].position;
        Vector3 previous = trackPosition;

        trackPosition = Vector3.MoveTowards(trackPosition, target, moveSpeed * Time.deltaTime);
        travelledDistance += Vector3.Distance(previous, trackPosition);

        Vector3 finalPos = trackPosition + Vector3.up * (Mathf.Sin(Time.time * bobFrequency) * bobAmount);
        transform.position = finalPos;

        Vector3 dir = target - trackPosition; //relatively position one to the other
        if (dir.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(dir.normalized, Vector3.up);//mean the direction you gonna turn to 
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationLerp * Time.deltaTime);//turn to another direction gradually
        }

        if (Vector3.Distance(trackPosition, target) <= 0.02f)
        {
            targetIndex++;

            if (targetIndex >= pathPoints.Length)
            {
                playing = false;
            }
        }
        UpdateDebugFields();
    }

    private void CalculateTotalDistance()
    {
        totalDistance = 0f;

        if (pathPoints == null || pathPoints.Length < 2) return;

        for (int i = 0; i < pathPoints.Length - 1; i++)
        {
            totalDistance += Vector3.Distance(pathPoints[i].position, pathPoints[i + 1].position);
        }
    }
    private void UpdateDebugFields()
    {
        debugNormalizedProgress = NormalizedProgress;
        debugTravelledDistance = travelledDistance;
        debugTotalDistance = totalDistance;
        debugCurrentTargetIndex = targetIndex;
        debugIsPlaying = playing;
    }
}