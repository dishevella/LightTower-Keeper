using UnityEngine;

public class ControllerAbstract : MonoBehaviour, IGameController
{
    
    IApp IBelongToApp.GetApp()
    {
        return GameApp.Interface;
    }
   
}
