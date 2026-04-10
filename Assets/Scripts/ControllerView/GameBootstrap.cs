using UnityEngine;

public class GameBootstrap : ControllerAbstract
{
    private void Awake()
    {
        
        (this as IBelongToApp).GetApp();
        Debug.Log("GameBootstrap Awake: App Initialized");
    }
    private void Start()
    {
        Debug.Log("GameBootstrap Start: StartGame");
        this.GetSystem<GameFlowSystem>().StartGame();
       
    }
}
