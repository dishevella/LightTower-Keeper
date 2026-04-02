using System.Collections;
using UnityEngine;

public class DockNpcSequenceController : MonoBehaviour
{
    [System.Serializable]
    public class ActorSequence
    {
        public Transform actor;
        public Transform[] waypoints;
        public float moveSpeed = 2f;
        public Animator animator;
        public string walkingBoolName = "IsWalking";
        public GameObject rootToDisableOnFinish;
    }

    [SerializeField] private ActorSequence[] actors;

    public bool IsPlaying { get; private set; }

    public void PlaySequence()
    {
        if (IsPlaying) return;
        StartCoroutine(PlayRoutine());
    }

    private IEnumerator PlayRoutine()
    {
        IsPlaying = true;

        if (actors == null || actors.Length == 0)
        {
            IsPlaying = false;
            yield break;
        }

        int completed = 0;

        for (int i = 0; i < actors.Length; i++)
        {
            StartCoroutine(MoveActorRoutine(actors[i], () => completed++));
        }

        yield return new WaitUntil(() => completed >= actors.Length);
        IsPlaying = false;
    }

    private IEnumerator MoveActorRoutine(ActorSequence actorSequence, System.Action onFinished)
    {
        if (actorSequence == null || actorSequence.actor == null)
        {
            onFinished?.Invoke();
            yield break;
        }

        SetWalking(actorSequence, true);

        if (actorSequence.waypoints != null)
        {
            for (int i = 0; i < actorSequence.waypoints.Length; i++)
            {
                var waypoint = actorSequence.waypoints[i];
                if (waypoint == null) continue;

                while (Vector3.Distance(actorSequence.actor.position, waypoint.position) > 0.05f)
                {
                    Vector3 nextPosition = Vector3.MoveTowards(
                        actorSequence.actor.position,
                        waypoint.position,
                        actorSequence.moveSpeed * Time.deltaTime);

                    Vector3 moveDirection = waypoint.position - actorSequence.actor.position;
                    moveDirection.y = 0f;

                    actorSequence.actor.position = nextPosition;

                    if (moveDirection.sqrMagnitude > 0.001f)
                    {
                        Quaternion targetRotation = Quaternion.LookRotation(moveDirection.normalized, Vector3.up);
                        actorSequence.actor.rotation = Quaternion.Slerp(actorSequence.actor.rotation, targetRotation, 10f * Time.deltaTime);
                    }

                    yield return null;
                }
            }
        }

        SetWalking(actorSequence, false);

        if (actorSequence.rootToDisableOnFinish != null)
        {
            actorSequence.rootToDisableOnFinish.SetActive(false);
        }

        onFinished?.Invoke();
    }

    private void SetWalking(ActorSequence actorSequence, bool walking)
    {
        if (actorSequence.animator != null && !string.IsNullOrEmpty(actorSequence.walkingBoolName))
        {
            actorSequence.animator.SetBool(actorSequence.walkingBoolName, walking);
        }
    }
}
