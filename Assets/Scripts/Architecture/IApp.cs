using UnityEngine;

public interface IApp
{
    ITypeEventSystem Events { get; }
    void SendCommand<T>() where T : class, IGameCommand, new();
    void SendCommand<T>(T command) where T : class, IGameCommand;
    void RegisterSystem<T>(T system) where T : class, IGameSystem;
    void RegisterModel<T>(T model) where T : class, IGameModel;

    T GetSystem<T>() where T : class, IGameSystem;
    T GetModel<T>() where T : class, IGameModel;
    void Initialize();
}