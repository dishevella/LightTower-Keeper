using System;
public class TypeEventSystemUnRegister<T> : IUnRegister
{
    public ITypeEventSystem TypeEventSystem { get; set; }
    public Action<T> OnEvent { get; set; }
    public void UnRegister()
    {
        TypeEventSystem?.UnRegister(OnEvent);
        TypeEventSystem = null;
        OnEvent = null;
    }
}
