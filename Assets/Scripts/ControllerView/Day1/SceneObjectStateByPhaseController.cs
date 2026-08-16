using UnityEngine;

using System;

public class SceneObjectStateByPhaseController : ControllerAbstract, ISceneStateApplier
{
    [Header("Legacy StoryPhase Compatibility")]
    [SerializeField] private StoryPhase[] activePhases;

    [Header("Chapter / Beat State")]
    [SerializeField] private StoryChapter[] activeChapters;
    [SerializeField] private string[] activeBeatIds;

    [Header("World Fact State")]
    [SerializeField] private string[] requiredWorldFacts;
    [SerializeField] private string[] excludedWorldFacts;

    [Header("Controlled Objects")]
    [SerializeField] private GameObject[] objectsToShowWhenActive;
    [SerializeField] private GameObject[] objectsToHideWhenActive;

    private void Awake()
    {
        this.GetEvent().Register<StoryPhaseChangedEvent>(OnStoryPhaseChanged)
            .UnRegisterWhenGameObjectDestroyed(gameObject);
        this.GetEvent().Register<StoryBeatEnteredEvent>(OnStoryBeatEntered)
            .UnRegisterWhenGameObjectDestroyed(gameObject);
        this.GetEvent().Register<WorldFactChangedEvent>(OnWorldFactChanged)
            .UnRegisterWhenGameObjectDestroyed(gameObject);

        ApplyCurrentState();
    }

    private void OnEnable()
    {
        ApplyCurrentState();
    }

    private void OnStoryPhaseChanged(StoryPhaseChangedEvent evt)
    {
        ApplyCurrentState();
    }

    private void OnStoryBeatEntered(StoryBeatEnteredEvent evt)
    {
        ApplyCurrentState();
    }

    private void OnWorldFactChanged(WorldFactChangedEvent evt)
    {
        ApplyCurrentState();
    }

    public void ApplyCurrentState()
    {
        bool isActivePhase = HasModernStateRules()
            ? IsCurrentModernStateMatched()
            : IsCurrentPhaseMatched();
        SetObjectsActive(objectsToShowWhenActive, isActivePhase);
        SetObjectsActive(objectsToHideWhenActive, !isActivePhase);
    }

    private bool HasModernStateRules()
    {
        return HasValues(activeChapters) || HasValues(activeBeatIds) ||
               HasValues(requiredWorldFacts) || HasValues(excludedWorldFacts);
    }

    private bool IsCurrentModernStateMatched()
    {
        StoryProgressModel progress = this.GetModel<StoryProgressModel>();
        WorldStateSystem facts = this.GetSystem<WorldStateSystem>();
        if (progress == null || facts == null)
        {
            return false;
        }

        if (HasValues(activeChapters) && !Contains(activeChapters, progress.CurrentChapter.Value))
        {
            return false;
        }

        if (HasValues(activeBeatIds) && !Contains(activeBeatIds, progress.CurrentBeatId.Value))
        {
            return false;
        }

        if (requiredWorldFacts != null)
        {
            foreach (string factId in requiredWorldFacts)
            {
                if (!string.IsNullOrWhiteSpace(factId) && !facts.HasFact(factId))
                {
                    return false;
                }
            }
        }

        if (excludedWorldFacts != null)
        {
            foreach (string factId in excludedWorldFacts)
            {
                if (!string.IsNullOrWhiteSpace(factId) && facts.HasFact(factId))
                {
                    return false;
                }
            }
        }

        return true;
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

    private static bool HasValues<T>(T[] values)
    {
        return values != null && values.Length > 0;
    }

    private static bool Contains(StoryChapter[] chapters, StoryChapter chapter)
    {
        foreach (StoryChapter candidate in chapters)
        {
            if (candidate == chapter) return true;
        }

        return false;
    }

    private static bool Contains(string[] values, string value)
    {
        foreach (string candidate in values)
        {
            if (!string.IsNullOrWhiteSpace(candidate) &&
                string.Equals(candidate.Trim(), value, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}
