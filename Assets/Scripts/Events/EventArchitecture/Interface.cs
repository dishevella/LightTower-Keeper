using System;
public interface ITypeEventSystem
{
    void Send<T>() where T : new();
    void Send<T>(T e);
    IUnRegister Register<T>(Action<T> onEvent);
    void UnRegister<T>(Action<T> onEvent);
}
 public interface IUnRegister
{
    void UnRegister();
}
