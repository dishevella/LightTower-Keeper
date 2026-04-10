using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class ShadowFigureController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject rootObject;
    [SerializeField] private Animator animator;
    [SerializeField] private Renderer[] targetRenderers;

    [Header("Movement")]
    [SerializeField] private Transform[] escapeWaypoints;
    [SerializeField] private float moveSpeed = 2.4f;
    [SerializeField] private float turnSpeed = 8f;
    [SerializeField] private float waypointTolerance = 0.05f;
    [SerializeField] private bool rotateOnlyOnYAxis = true;

    [Header("Animation")]
    [SerializeField] private string turnStateName = "Turning";
    [SerializeField] private string sprintStateName = "Sprint";
    [SerializeField] private float animationCrossFade = 0.08f;
    [SerializeField] private float turnDuration = 0.45f;

    [Header("Disappear")]
    [SerializeField] private bool dissolveOnFinish = true;
    [SerializeField] private string dissolvePropertyName = "_Dissolve";
    [SerializeField] private float dissolveDuration = 0.25f;
    [SerializeField] private bool hideRootOnFinish = true;
    [SerializeField] private bool startHidden = true;

    [Header("Events")]
    [SerializeField] private UnityEvent onEscapeStarted;
    [SerializeField] private UnityEvent onEscapeFinished;

    private int dissolvePropertyId;
    private Coroutine activeRoutine;
    private MaterialPropertyBlock propertyBlock;
    private float currentDissolve;

    private void Awake()
    {
        if (rootObject == null)
        {
            rootObject = gameObject;
        }

        if (targetRenderers == null || targetRenderers.Length == 0)
        {
            targetRenderers = rootObject.GetComponentsInChildren<Renderer>(true);
        }

        dissolvePropertyId = Shader.PropertyToID(dissolvePropertyName);
        propertyBlock = new MaterialPropertyBlock();

        SetDissolveImmediate(0f);

        if (startHidden && rootObject != null)
        {
            rootObject.SetActive(false);
        }
    }

    public void ShowImmediate()
    {
        if (rootObject != null)
        {
            rootObject.SetActive(true);
        }

        SetDissolveImmediate(0f);
    }

    public void HideImmediate()
    {
        if (activeRoutine != null)
        {
            StopCoroutine(activeRoutine);
            activeRoutine = null;
        }

        SetDissolveImmediate(1f);

        if (rootObject != null && hideRootOnFinish)
        {
            rootObject.SetActive(false);
        }
    }

    public void BeginEscape()
    {
        if (activeRoutine != null)
        {
            StopCoroutine(activeRoutine);
        }

        activeRoutine = StartCoroutine(EscapeRoutine());
    }

    private IEnumerator EscapeRoutine()
    {
        if (rootObject == null)
        {
            yield break;
        }

        rootObject.SetActive(true);
        SetDissolveImmediate(0f);
        onEscapeStarted?.Invoke();

        Transform firstWaypoint = GetFirstValidWaypoint();
        if (firstWaypoint != null)
        {
            PlayState(turnStateName);

            if (turnDuration > 0f)
            {
                yield return TurnTowardsWaypoint(firstWaypoint, turnDuration);
            }
            else
            {
                RotateTowardsWaypointImmediate(firstWaypoint);
            }
        }

        PlayState(sprintStateName);

        if (escapeWaypoints != null)
        {
            for (int i = 0; i < escapeWaypoints.Length; i++)
            {
                Transform waypoint = escapeWaypoints[i];
                if (waypoint == null)
                {
                    continue;
                }

                while (!MoveTowardsWaypoint(waypoint))
                {
                    yield return null;
                }
            }
        }

        if (dissolveOnFinish)
        {
            yield return DissolveTo(1f, dissolveDuration);
        }

        if (rootObject != null && hideRootOnFinish)
        {
            rootObject.SetActive(false);
        }

        onEscapeFinished?.Invoke();
        activeRoutine = null;
    }

    private Transform GetFirstValidWaypoint()
    {
        if (escapeWaypoints == null)
        {
            return null;
        }

        for (int i = 0; i < escapeWaypoints.Length; i++)
        {
            if (escapeWaypoints[i] != null)
            {
                return escapeWaypoints[i];
            }
        }

        return null;
    }

    private bool MoveTowardsWaypoint(Transform waypoint)
    {
        Vector3 currentPosition = rootObject.transform.position;
        Vector3 targetPosition = waypoint.position;

        Vector3 nextPosition = Vector3.MoveTowards(
            currentPosition,
            targetPosition,
            moveSpeed * Time.deltaTime);

        rootObject.transform.position = nextPosition;

        Vector3 direction = targetPosition - currentPosition;
        if (rotateOnlyOnYAxis)
        {
            direction.y = 0f;
        }

        if (direction.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            rootObject.transform.rotation = Quaternion.Slerp(
                rootObject.transform.rotation,
                targetRotation,
                turnSpeed * Time.deltaTime);
        }

        return Vector3.Distance(nextPosition, targetPosition) <= waypointTolerance;
    }

    private IEnumerator TurnTowardsWaypoint(Transform waypoint, float duration)
    {
        Quaternion startRotation = rootObject.transform.rotation;
        Quaternion targetRotation = GetTargetRotation(waypoint.position);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            rootObject.transform.rotation = Quaternion.Slerp(startRotation, targetRotation, t);
            yield return null;
        }

        rootObject.transform.rotation = targetRotation;
    }

    private void RotateTowardsWaypointImmediate(Transform waypoint)
    {
        rootObject.transform.rotation = GetTargetRotation(waypoint.position);
    }

    private Quaternion GetTargetRotation(Vector3 targetPosition)
    {
        Vector3 direction = targetPosition - rootObject.transform.position;
        if (rotateOnlyOnYAxis)
        {
            direction.y = 0f;
        }

        if (direction.sqrMagnitude <= 0.0001f)
        {
            return rootObject.transform.rotation;
        }

        return Quaternion.LookRotation(direction.normalized, Vector3.up);
    }

    private IEnumerator DissolveTo(float targetValue, float duration)
    {
        float startValue = currentDissolve;

        if (duration <= 0f)
        {
            SetDissolveImmediate(targetValue);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            SetDissolveImmediate(Mathf.Lerp(startValue, targetValue, t));
            yield return null;
        }

        SetDissolveImmediate(targetValue);
    }

    private void PlayState(string stateName)
    {
        if (animator == null)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(stateName) && animator.HasState(0, Animator.StringToHash(stateName)))
        {
            animator.CrossFadeInFixedTime(stateName, animationCrossFade);
            return;
        }
    }

    private void SetDissolveImmediate(float value)
    {
        currentDissolve = Mathf.Clamp01(value);

        if (targetRenderers == null)
        {
            return;
        }

        for (int i = 0; i < targetRenderers.Length; i++)
        {
            Renderer targetRenderer = targetRenderers[i];
            if (targetRenderer == null)
            {
                continue;
            }

            targetRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetFloat(dissolvePropertyId, currentDissolve);
            targetRenderer.SetPropertyBlock(propertyBlock);
        }
    }
}
