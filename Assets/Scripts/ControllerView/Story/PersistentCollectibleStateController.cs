using System;
using UnityEngine;

public sealed class PersistentCollectibleStateController : ControllerAbstract, ISceneStateApplier
{
    [SerializeField] private string collectibleId;
    [SerializeField] private GameObject collectibleRoot;

    private void Awake()
    {
        this.GetEvent().Register<CollectibleStateChangedEvent>(OnCollectibleStateChanged)
            .UnRegisterWhenGameObjectDestroyed(gameObject);
    }

    private void OnEnable()
    {
        ApplyCurrentState();
    }

    public void MarkCollected()
    {
        this.GetSystem<CollectibleStateSystem>()?.Collect(collectibleId);
        ApplyCurrentState();
    }

    public void ApplyCurrentState()
    {
        bool collected = this.GetSystem<CollectibleStateSystem>()?.IsCollected(collectibleId) == true;
        GameObject target = collectibleRoot != null ? collectibleRoot : gameObject;
        if (target.activeSelf == collected)
        {
            target.SetActive(!collected);
        }
    }

    private void OnCollectibleStateChanged(CollectibleStateChangedEvent evt)
    {
        if (string.Equals(evt.CollectibleId, collectibleId, StringComparison.Ordinal))
        {
            ApplyCurrentState();
        }
    }
}
