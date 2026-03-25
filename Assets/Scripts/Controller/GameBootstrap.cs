using UnityEngine;

public class GameBootstrap : ControllerAbstract
{
    private void Awake()
    {
        GameApp.Instance.Initialize();
        Debug.Log("GameBootstrap Awake: App Initialized");
    }
    private void Start()
    {
        Debug.Log("GameBootstrap Start: StartGame");
        GameApp.Instance.GetSystem<GameFlowSystem>().StartGame();
    }
}
