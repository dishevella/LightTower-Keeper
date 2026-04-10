using UnityEngine;

public class Day1WakeUpAnimationEventRelay : ControllerAbstract
{
    [SerializeField] private Day1WakeUpController day1WakeUpController;

    public void OnReachGrabCommunicationDevice()
    {
        if (day1WakeUpController != null)
        {
            day1WakeUpController.OnReachGrabCommunicationDevice();
        }
    }

   

    public void OnCheckingAnimationFinished()
    {
        if (day1WakeUpController != null)
        {
            day1WakeUpController.OnCheckingAnimationFinished();
        }
    }
}
