using System;
using UnityEngine;

public class TimeOfDayDriver : ControllerAbstract
{
    [Serializable]
    public class PhaseTimeMapping
    {
        public StoryPhase phase;
        [Range(0f, 24f)] public float targetHour = 12f;
    }

    [Header("Initialization")]
    [SerializeField] private bool initializeOnStart = true;
    [Range(0f, 24f)]
    [SerializeField] private float initialHour = 6f;

    [Header("Phase Mapping")]
    [SerializeField] private PhaseTimeMapping[] phaseMappings;

    private void Start()
    {
        if (initializeOnStart)
        {
            this.SendCommand(new SetTimeOfDayCommand(initialHour));
        }

        this.GetEvent().Register<StoryPhaseChangedEvent>(OnStoryPhaseChanged)
            .UnRegisterWhenGameObjectDestroyed(gameObject);
    }

    private void OnStoryPhaseChanged(StoryPhaseChangedEvent evt)
    {
        if (phaseMappings == null || phaseMappings.Length == 0) return;

        for (int i = 0; i < phaseMappings.Length; i++)
        {
            PhaseTimeMapping mapping = phaseMappings[i];
            if (mapping == null) continue;
            if (mapping.phase != evt.NewPhase) continue;

            this.SendCommand(new SetTimeOfDayCommand(mapping.targetHour));

            break;
        }
    }
}
