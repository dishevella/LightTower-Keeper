using JetBrains.Annotations;
using System;
using System.Collections.Generic;

public class EventBus
{
    private readonly Dictionary<Type, List<Delegate>> listeners = new Dictionary<Type, List<Delegate>>();
    public void Subscribe<T>(Action<T> listener)
    {
        var type = typeof(T);
        if (!listeners.TryGetValue(type, out var list))
        {
            list = new List<Delegate>();
            listeners[type] = list;
        }
        if(!list.Contains(listener))
        {
            list.Add(listener);
        }
    }
    public void Unsubscribe<T>(Action<T> listener)
    {
        var type = typeof(T);
        if (!listeners.TryGetValue(type, out var list)) return;
        list.Remove(listener);
        if(list.Count == 0)
        {
            listeners.Remove(type);
        }
    }
    public void Publish<T>(T evt)
    {
        var type = typeof(T);
        if (!listeners.TryGetValue(type, out var list)) return;
        var snapshot = list.ToArray();
        foreach(var item in snapshot)
        {
            if(item is Action<T> callback)
            {
                callback.Invoke(evt);
            }
        }
    }
}