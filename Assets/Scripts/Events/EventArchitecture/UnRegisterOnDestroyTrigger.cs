using UnityEngine;
using System.Collections.Generic;

public class UnRegisterOnDestroyTrigger : MonoBehaviour
{
    private readonly HashSet<IUnRegister> unRegisters = new HashSet<IUnRegister>();
    public void AddUnRegister(IUnRegister unRegister)
    {
        if(unRegister != null)
        {
            unRegisters.Add(unRegister);
        }
    }
    private void OnDestroy()
    {
        foreach(var unRegister in unRegisters)
        {
            unRegister.UnRegister();
        }
        unRegisters.Clear();
    }
}
