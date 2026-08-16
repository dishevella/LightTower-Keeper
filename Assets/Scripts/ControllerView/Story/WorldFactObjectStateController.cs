using System;
using UnityEngine;

public sealed class WorldFactObjectStateController : ControllerAbstract, ISceneStateApplier
{
    [SerializeField] private string worldFactId;
    [SerializeField] private bool activeWhenFactIsSet = true;
    [SerializeField] private GameObject[] controlledObjects;

    private void Awake()
    {
        this.GetEvent().Register<WorldFactChangedEvent>(OnWorldFactChanged)
            .UnRegisterWhenGameObjectDestroyed(gameObject);
    }

    private void OnEnable()
    {
        ApplyCurrentState();
    }

    public void ApplyCurrentState()
    {
        bool factIsSet = this.GetSystem<WorldStateSystem>()?.HasFact(worldFactId) == true;
        SetObjectsActive(factIsSet == activeWhenFactIsSet);
    }

    private void OnWorldFactChanged(WorldFactChangedEvent evt)
    {
        if (string.Equals(evt.FactId, worldFactId, StringComparison.Ordinal))
        {
            ApplyCurrentState();
        }
    }

    private void SetObjectsActive(bool active)
    {
        if (controlledObjects == null) return;

        foreach (GameObject controlledObject in controlledObjects)
        {
            if (controlledObject != null)
            {
                controlledObject.SetActive(active);
            }
        }
    }
}
