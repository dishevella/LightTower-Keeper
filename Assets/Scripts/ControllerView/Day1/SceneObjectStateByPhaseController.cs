using UnityEngine;

public class SceneObjectStateByPhaseController : ControllerAbstract
{
    [SerializeField] private StoryPhase[] activePhases;
    [SerializeField] private GameObject[] objectsToShowWhenActive;
    [SerializeField] private GameObject[] objectsToHideWhenActive;

    private void Awake()
    {
        this.GetEvent().Register<StoryPhaseChangedEvent>(OnStoryPhaseChanged)
            .UnRegisterWhenGameObjectDestroyed(gameObject);

        Refresh();
    }

    private void OnStoryPhaseChanged(StoryPhaseChangedEvent evt)
    {
        Refresh();
    }

    private void Refresh()
    {
        bool isActivePhase = IsCurrentPhaseMatched();
        SetObjectsActive(objectsToShowWhenActive, isActivePhase);
        SetObjectsActive(objectsToHideWhenActive, !isActivePhase);
    }

    private bool IsCurrentPhaseMatched()
    {
        var gameState = this.GetModel<GameStateModel>();
        if (gameState == null) return false;

        if (activePhases == null || activePhases.Length == 0)
            return false;

        var currentPhase = gameState.CurrentPhase.Value;
        for (int i = 0; i < activePhases.Length; i++)
        {
            if (activePhases[i] == currentPhase)
                return true;
        }

        return false;
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
