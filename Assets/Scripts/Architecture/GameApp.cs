using System;
using System.Collections.Generic;


public sealed class GameApp // sealed means can not be inherited anymore
{
    private static readonly GameApp instance = new GameApp(); // readonly means you only can give it a value when you define it or in the constructor function
    public static GameApp Instance => instance;
    private readonly Dictionary<Type, IGameSystem> systems = new();
    private readonly Dictionary<Type, IGameModel> models = new();
    public EventBus Events { get; }
    public CommandDispatcher Commands { get; }

    private bool initialized;
    private GameApp()
    {
        Events = new EventBus();
        Commands = new CommandDispatcher(this);
    }
    public void Initialize()
    {
        if (initialized) return;
        RegisterModel(new GameStateModel());
        RegisterModel(new TaskModel());

        RegisterSystem(new TaskSystem());
        RegisterSystem(new TimeSystem());
        RegisterSystem(new GameFlowSystem());

        foreach (var system in systems.Values)
        {
            system.Initialize(this);
        }
        initialized = true;
    }
    public void RegisterSystem<T>(T system) where T : class, IGameSystem
    {
        systems[typeof(T)] = system;
    }
    public void RegisterModel<T>(T model) where T : class, IGameModel
    {
        models[typeof(T)] = model;
    }
    public T GetSystem<T>() where T : class, IGameSystem
    {
        if(systems.TryGetValue(typeof(T),out var system))
        {
            return system as T;
        }
        return null;
    }
    public T GetModel<T>() where T:class, IGameModel
    {
        if(models.TryGetValue(typeof(T),out var model))
        {
            return model as T;
        }
        return null;
    }
}
