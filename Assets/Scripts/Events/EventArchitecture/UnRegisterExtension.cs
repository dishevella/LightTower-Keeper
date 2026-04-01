using UnityEngine;

public static class UnRegisterExtension
{
    public static void UnRegisterWhenGameObjectDestroyed(this IUnRegister unRegister,GameObject gameobject)
    {
        if (unRegister == null || gameobject == null) return;
        var trigger = gameobject.GetComponent<UnRegisterOnDestroyTrigger>();
        if(trigger == null)
        {
            trigger = gameobject.AddComponent<UnRegisterOnDestroyTrigger>();
        }
        trigger.AddUnRegister(unRegister);
    }
}
