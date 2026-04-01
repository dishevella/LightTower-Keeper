using System;
using System.Collections.Generic;
public class TypeEventSystem : ITypeEventSystem
{
    private interface IRegistrations { }
    private class Registrations<T> : IRegistrations
    {
        public Action<T> OnEvent = _ => { };
    }
    private readonly Dictionary<Type, IRegistrations> eventRegistrations =
        new Dictionary<Type, IRegistrations>();
    public void Send<T>() where T: new()
    {
        Send(new T());
    }
    public void Send<T>(T e)
    {
        var type = typeof(T);
        if (eventRegistrations.TryGetValue(type, out var registrations))
        {
            (registrations as Registrations<T>)?.OnEvent.Invoke(e);
        }
    }
    public IUnRegister Register<T>(Action<T> onEvent)
    {
        var type = typeof(T);

        if (!eventRegistrations.TryGetValue(type, out var registrations))
        {
            registrations = new Registrations<T>();
            eventRegistrations.Add(type, registrations);
        }

        (registrations as Registrations<T>).OnEvent += onEvent;

        return new TypeEventSystemUnRegister<T>
        {
            TypeEventSystem = this,
            OnEvent = onEvent
        };
    }
    public void UnRegister<T>(Action<T> onEvent)
    {
        var type = typeof(T);
        if(eventRegistrations.TryGetValue(type,out var registrations))
        {
            (registrations as Registrations<T>).OnEvent -= onEvent;
        }
    }
}