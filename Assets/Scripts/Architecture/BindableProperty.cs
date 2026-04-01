using System;

    public class BindableProperty<T> 
{
    private T value;
    public event Action<T> OnValueChanged;
    public T Value
    {
        get => value;
        set
        {
            if (Equals(this.value, value)) return;
            this.value = value;
            OnValueChanged?.Invoke(this.value);
        }
    }
    public BindableProperty(T defaultValue = default)
    {
        value = defaultValue;
    }
    public void SetValueWithoutNotify(T newValue)
    {
        value = newValue;
    }
}
