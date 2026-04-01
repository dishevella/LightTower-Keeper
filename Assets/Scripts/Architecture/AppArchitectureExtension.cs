using System.Windows.Input;
using UnityEngine;

public static class AppArchitectureExtension
{
    public static T GetSystem<T>(this ICanGetSystem self) where T: class,IGameSystem
    {
        return self.GetApp().GetSystem<T>();
    }
    public static T GetModel<T>(this ICanGetModel self) where T : class, IGameModel
    {
        return self.GetApp().GetModel<T>();
    }
    public static void SendCommand<T>(this ICanSendCommand self) where T :class, IGameCommand, new()
    {
        self.GetApp().SendCommand<T>();
    }

    public static void SendCommand<T>(this ICanSendCommand self, T command) where T : class,IGameCommand
    {
        self.GetApp().SendCommand(command);
    }

    public static ITypeEventSystem GetEvent(this ICanGetEvent self)
    {
        return self.GetApp().Events;
    }
}
