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

    [Header("Look Development")]
    [SerializeField] private bool lockTimeForLookDevelopment;
    [Range(0f, 24f)]
    [SerializeField] private float lookDevelopmentHour = 17.75f;

    [Header("Phase Mapping")]
    [SerializeField] private PhaseTimeMapping[] phaseMappings;

    private void Start()
    {
        if (initializeOnStart)
        {
            float startHour = lockTimeForLookDevelopment ? lookDevelopmentHour : initialHour;
            this.SendCommand(new SetTimeOfDayCommand(startHour));
        }

        this.GetEvent().Register<StoryPhaseChangedEvent>(OnStoryPhaseChanged)
            .UnRegisterWhenGameObjectDestroyed(gameObject);
    }

    private void OnStoryPhaseChanged(StoryPhaseChangedEvent evt)
    {
        if (lockTimeForLookDevelopment) return;
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

    public void ConfigureLookDevelopmentTime(bool locked, float hour)
    {
        lockTimeForLookDevelopment = locked;
        lookDevelopmentHour = TimeSystem.WrapHour(hour);
        if (locked)
            initialHour = lookDevelopmentHour;
    }
}
