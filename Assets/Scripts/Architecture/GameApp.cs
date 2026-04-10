using System;
using System.Collections.Generic;
using System.Windows.Input;


public sealed class GameApp : IApp// sealed means can not be inherited anymore
{
    private static GameApp instance; // readonly means you only can give it a value when you define it or in the constructor function

    private readonly Dictionary<Type, IGameSystem> systems = new();
    private readonly Dictionary<Type, IGameModel> models = new();

    private readonly List<IGameModel> pendingModels = new();
    private readonly List<IGameSystem> pendingSystems = new();
    public ITypeEventSystem Events { get; }
   

    private bool initialized;
    private GameApp()
    {
        Events = new TypeEventSystem();
        
    }
    public static IApp Interface
    { 
        get
        {
            if(instance == null)
            {
                instance = new GameApp();
                instance.Initialize();
            }
            return instance;
        }
    }
    public void Initialize()
    {

        if (initialized) return;
        RegisterModel(new GameStateModel());
        RegisterModel(new TaskModel());
        RegisterModel(new ToolInventoryModel());
        RegisterModel(new LighthouseDutyModel());
        RegisterModel(new InventoryModel());
        RegisterModel(new Day1RouteModel());
        RegisterModel(new TimeOfDayModel());

        RegisterSystem(new TaskSystem());
        RegisterSystem(new TimeSystem());
        RegisterSystem(new GameFlowSystem());
        RegisterSystem(new InventorySystem());

        foreach (var model in pendingModels)
        {  
            model.Initialize();
        }
        pendingModels.Clear();
        foreach (var system in pendingSystems)
        {
            system.Initialize();
        }
        pendingSystems.Clear();
        initialized = true;

    }


    public void RegisterSystem<T>(T system) where T : class, IGameSystem
    {
        system.SetApp(this);
        systems[typeof(T)] = system;
        if(initialized)
        {
            system.Initialize();
        }
        else
        {
            pendingSystems.Add(system);
        }
    }
    public void RegisterModel<T>(T model) where T : class, IGameModel
    {
        model.SetApp(this);
        models[typeof(T)] = model;
        if(initialized)
        {
            model.Initialize();
        }
        else
        {
            pendingModels.Add(model);
        }
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
    public void SendCommand<T>() where T :class, IGameCommand, new()
    {
        var command = new T();
        command.SetApp(this);
        command.Execute();
    }
    public void SendCommand<T>(T command) where T :class, IGameCommand
    {
        command.SetApp(this);
        command.Execute();
    }
}
