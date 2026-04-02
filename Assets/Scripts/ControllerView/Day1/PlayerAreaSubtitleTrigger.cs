using UnityEngine;

public class PlayerAreaSubtitleTrigger : MonoBehaviour
{
    [SerializeField] private StoryPhase[] availablePhases;
    [SerializeField] private WorldSubtitleView subtitleView;
    [SerializeField] private bool onlyOnce = true;

    private bool triggered;

    private void OnTriggerEnter(Collider other)
    {
        if (triggered && onlyOnce) return;
        if (other.GetComponentInParent<PlayerController>() == null) return;
        if (!IsPhaseAllowed()) return;

        if (subtitleView != null)
        {
            subtitleView.ResetView();
            subtitleView.Play();
        }

        if (onlyOnce)
        {
            triggered = true;
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
