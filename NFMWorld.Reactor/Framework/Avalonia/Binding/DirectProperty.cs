using System.Diagnostics;

namespace WorldXaml.UI.Base;

internal interface IDirectProperty
{
    int Id { get; }
    string Name { get; }
    Type PropertyType { get; }
    Type OwnerType { get; }
    object? DefaultValue { get; }
    Action<PropertyObject, object?>? OnChanged { get; }
    
    void SetBoxedDirectValue(PropertyObject target, object? value);
}

public abstract class BaseDirectProperty<TValue>(
    string name,
    Type ownerType,
    TValue defaultValue,
    Action<PropertyObject, TValue>? onChanged = null)
    : Property<TValue>(name, ownerType, defaultValue, onChanged), IDirectProperty
{
    /// <summary>Read the backing value via the DirectProperty getter.</summary>
    internal abstract TValue? GetDirectValue(PropertyObject target);
    
    /// <summary>Write the backing value via the DirectProperty setter.</summary>
    internal abstract void SetDirectValue(PropertyObject target, TValue? value);

    void IDirectProperty.SetBoxedDirectValue(PropertyObject target, object? value)
    {
        Debug.Assert(value == null || PropertyType.IsInstanceOfType(value), $"Value of type {value?.GetType()} is not assignable to property {Name} of type {PropertyType}.");

        SetDirectValue(target, (TValue?)value);
    }
}

public sealed class DirectProperty<TOwner, TValue> : BaseDirectProperty<TValue>
    where TOwner : PropertyObject
{
    public Func<TOwner, TValue> Getter { get; }
    public Action<TOwner, TValue>? Setter { get; }

    internal DirectProperty(
        string name,
        Func<TOwner, TValue> getter,
        Action<TOwner, TValue>? setter,
        TValue defaultValue)
        : base(name, typeof(TOwner), defaultValue)
    {
        Getter = getter;
        Setter = setter;
    }

    internal override TValue? GetDirectValue(PropertyObject target) => Getter((TOwner)target);

    internal override void SetDirectValue(PropertyObject target, TValue? value) =>
        (Setter ?? throw new InvalidOperationException($"Property '{Name}' is read-only."))((TOwner)target, (TValue)value!);
}