using UnityEngine;
using System;
using System.Collections;

public class TimeSystem : MonoBehaviour
{
    public static TimeSystem Instance { get; private set; }
    [SerializeField] private DayTransitionPanel dayTransitionpanel;
    public int CurrentDay { get; private set; } = 0;
    public event Action<int> OnDayChanged;
    private bool isTransitioning;
    private void Awake()
    {
        if(Instance!=null&& Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
    public void SetDay(int day)
    {
        CurrentDay = day;
        OnDayChanged?.Invoke(CurrentDay);
    }
    public void GoToNextDay(Action onFinished = null)
    {
        if (isTransitioning) return;
        StartCoroutine(GoToNextDayRoutine(onFinished));
    }
    private IEnumerator GoToNextDayRoutine(Action onFinished)
    {
        isTransitioning = true;

        CurrentDay++;

        if (dayTransitionpanel != null)
        {
            yield return dayTransitionpanel.PlayTransition(CurrentDay);
        }

        OnDayChanged?.Invoke(CurrentDay);

        onFinished?.Invoke();

        isTransitioning = false;
    }

}
