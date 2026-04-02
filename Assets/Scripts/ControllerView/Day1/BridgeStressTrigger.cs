using System.Collections;
using UnityEngine;

public class BridgeStressTrigger : MonoBehaviour
{
    [SerializeField] private StoryPhase[] availablePhases;
    [SerializeField] private AudioSource stressAudioSource;
    [SerializeField] private Transform shakeTarget;
    [SerializeField] private float shakeDuration = 0.35f;
    [SerializeField] private float shakeStrength = 0.03f;
    [SerializeField] private bool onlyOnce = true;

    private bool triggered;

    private void OnTriggerEnter(Collider other)
    {
        if (triggered && onlyOnce) return;
        if (other.GetComponentInParent<PlayerController>() == null) return;
        if (!IsPhaseAllowed()) return;

        if (stressAudioSource != null)
        {
            stressAudioSource.Play();
        }

        if (shakeTarget != null)
        {
            StartCoroutine(ShakeRoutine());
        }

        if (onlyOnce)
        {
            triggered = true;
        }
    }

    private IEnumerator ShakeRoutine()
    {
        Vector3 originalPosition = shakeTarget.localPosition;
        float time = 0f;

        while (time < shakeDuration)
        {
            time += Time.deltaTime;
            shakeTarget.localPosition = originalPosition + Random.insideUnitSphere * shakeStrength;
            yield return null;
        }

        shakeTarget.localPosition = originalPosition;
    }

    private bool IsPhaseAllowed()
    {
        if (availablePhases == null || availablePhases.Length == 0)
            return true;

        var gameState = GameApp.Interface.GetModel<GameStateModel>();
        if (gameState == null) return false;

        var currentPhase = gameState.CurrentPhase.Value;
        for (int i = 0; i < availablePhases.Length; i++)
        {
            if (availablePhases[i] == currentPhase)
                return true;
        }

        return false;
    }
}
