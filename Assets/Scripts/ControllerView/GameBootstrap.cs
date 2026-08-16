using UnityEngine;

public class GameBootstrap : ControllerAbstract
{
    [Header("Startup")]
    [SerializeField] private bool continueFromSaveWhenAvailable;
    [SerializeField] private bool attachGameModeSceneBridge = true;

    private void Awake()
    {
        (this as IBelongToApp).GetApp();
        if (attachGameModeSceneBridge && GetComponent<GameModeSceneBridge>() == null)
        {
            gameObject.AddComponent<GameModeSceneBridge>();
        }

        Debug.Log("GameBootstrap Awake: App Initialized");
    }
    private void Start()
    {
        if (continueFromSaveWhenAvailable && this.GetSystem<SaveSystem>()?.HasDefaultSave == true)
        {
            Debug.Log("GameBootstrap Start: Continue from checkpoint");
            if (this.GetSystem<SaveSystem>().TryLoadDefaultSlot())
            {
                return;
            }

            Debug.LogWarning("Continue failed. Starting a new game instead.", this);
        }

        StartNewGame();
    }

    public void StartNewGame()
    {
        Debug.Log("GameBootstrap: Start new game");
        this.GetSystem<GameFlowSystem>()?.StartGame();
    }

    public bool ContinueGame()
    {
        return this.GetSystem<SaveSystem>()?.TryLoadDefaultSlot() == true;
    }
}
