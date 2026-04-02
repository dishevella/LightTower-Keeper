using System.Collections;
using UnityEngine;

public class SimpleOneShotCommentTrigger : MonoBehaviour
{
    [SerializeField] private StoryPhase[] availablePhases;
    [SerializeField] private ScreenSubtitlePanel screenSubtitlePanel;
    [TextArea(2, 4)]
    [SerializeField] private string line;
    [SerializeField] private float fadeIn = 0.35f;
    [SerializeField] private float hold = 2.2f;
    [SerializeField] private float fadeOut = 0.45f;
    [SerializeField] private bool onlyOnce = true;

    private bool triggered;

    private void OnTriggerEnter(Collider other)
    {
        if (triggered && onlyOnce) return;
        if (other.GetComponentInParent<PlayerController>() == null) return;
        if (!IsPhaseAllowed()) return;

        StartCoroutine(PlayCommentRoutine());

        if (onlyOnce)
        {
            triggered = true;
        }
    }

    private IEnumerator PlayCommentRoutine()
    {
        if (screenSubtitlePanel != null)
        {
            yield return screenSubtitlePanel.PlayLine(line, fadeIn, hold, fadeOut);
        }
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
