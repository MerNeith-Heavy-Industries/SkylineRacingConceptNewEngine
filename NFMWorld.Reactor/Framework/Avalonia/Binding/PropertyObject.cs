using System.ComponentModel;
using System.Diagnostics;
using WorldXaml.UI.Base;

namespace WorldXaml.UI;

public readonly record struct StyledPropertyChangedEventArgs(Property Property, object? OldValue, object? NewValue);

public abstract class PropertyObject : INotifyPropertyChanging, INotifyPropertyChanged
{
    // Stores local values
    private readonly Dictionary<int, object?> _values = new();

    public event EventHandler<StyledPropertyChangedEventArgs>? StyledPropertyChanged;
    public event PropertyChangingEventHandler? PropertyChanging;
    public event PropertyChangedEventHandler? PropertyChanged;

    [EditorBrowsable(EditorBrowsableState.Never)]
    public TValue GetValue<TValue>(Property<TValue> property)
    {
        if (property is BaseDirectProperty<TValue> directProperty)
            return directProperty.GetDirectValue(this)!;
        if (_values.TryGetValue(property.Id, out var raw))
            return (TValue)raw!;
        return property.DefaultValue;
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public void SetValue<TValue>(Property<TValue> property, TValue value)
    {
        var oldValue = GetValue(property);
            
        PropertyChanging?.Invoke(this, property.CachedChangingArgs);

        if (property is BaseDirectProperty<TValue> directProperty)
            directProperty.SetDirectValue(this, value);
        else
            _values[property.Id] = value;

        PropertyChanged?.Invoke(this, property.CachedChangedArgs);

        property.OnChanged?.Invoke(this, value);

        if (!EqualityComparer<TValue>.Default.Equals(oldValue, value))
            StyledPropertyChanged?.Invoke(this, new StyledPropertyChangedEventArgs(property, oldValue, value));
    }

    /// <summary>
    /// Sets a property value with an untyped (boxed) value.
    /// Used by Style setters and the Reactor reconciler.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public void SetBoxedValue(Property property, object? value)
    {
        Debug.Assert(value == null || property.PropertyType.IsInstanceOfType(value), $"Value of type {value?.GetType()} is not assignable to property {property.Name} of type {property.PropertyType}.");

        var oldValue = _values.TryGetValue(property.Id, out var oldVal) ? oldVal : property.DefaultValue;
        
        PropertyChanging?.Invoke(this, property.CachedChangingArgs);

        if (property is IDirectProperty directProperty)
            directProperty.SetBoxedDirectValue(this, value);
        else
            _values[property.Id] = value;
        
        PropertyChanged?.Invoke(this, property.CachedChangedArgs);

        property.OnChanged?.Invoke(this, value);

        if (!Equals(oldValue, value))
            StyledPropertyChanged?.Invoke(this, new StyledPropertyChangedEventArgs(property, oldValue, value));
    }
}

public class PropertyRegistry
{
    public static readonly PropertyRegistry Instance = new();
    private readonly Dictionary<Type, List<Property>> _registered = new();

    public void Register(Type ownerType, Property property)
    {
        if (!_registered.TryGetValue(ownerType, out var list))
            _registered[ownerType] = list = [];
        list.Add(property);
    }

    public IEnumerable<Property> GetRegistered(Type type)
    {
        foreach (var (ownerType, props) in _registered)
            if (ownerType.IsAssignableFrom(type))
                foreach (var prop in props)
                    yield return prop;
    }

    /// <summary>
    /// Finds a registered property by its unique integer ID.
    /// Used by the Reactor reconciler for VNode→native mapping.
    /// </summary>
    public Property? FindById(int id)
    {
        foreach (var (_, props) in _registered)
        foreach (var p in props)
            if (p.Id == id)
                return p;
        return null;
    }
}

public class Property
{
    private static int _nextId;
    public int Id { get; } = _nextId++;
    public string Name { get; }
    public Type PropertyType { get; }
    public Type OwnerType { get; }
    public object? DefaultValue { get; }
    public Action<PropertyObject, object?>? OnChanged { get; }

    internal readonly PropertyChangingEventArgs CachedChangingArgs;
    internal readonly PropertyChangedEventArgs CachedChangedArgs;

    private protected Property(string name, Type propertyType, Type ownerType, object? defaultValue,
        Action<PropertyObject, object?>? onChanged = null)
    {
        Name = name;
        PropertyType = propertyType;
        OwnerType = ownerType;
        DefaultValue = defaultValue;
        OnChanged = onChanged;
        CachedChangingArgs = new PropertyChangingEventArgs(name);
        CachedChangedArgs = new PropertyChangedEventArgs(name);
    }

    public static Property<TValue> Register<TOwner, TValue>(string name, TValue defaultValue = default!,
        Action<TOwner, TValue>? onChanged = null)
        where TOwner : PropertyObject
    {
        Action<PropertyObject, TValue>? wrapped = onChanged is null
            ? null
            : (obj, val) => onChanged((TOwner)obj, val);

        var prop = new Property<TValue>(name, typeof(TOwner), defaultValue, wrapped);
        PropertyRegistry.Instance.Register(typeof(TOwner), prop);
        return prop;
    }

    public override bool Equals(object? obj) => obj is Property p && p.Id == Id;
    public override int GetHashCode() => Id;

    public static DirectProperty<TOwner, TValue> RegisterDirect<TOwner, TValue>(
        string name,
        Func<TOwner, TValue> getter,
        Action<TOwner, TValue>? setter = null,
        TValue defaultValue = default!) where TOwner : PropertyObject
    {
        var prop = new DirectProperty<TOwner, TValue>(name, getter, setter, defaultValue);
        PropertyRegistry.Instance.Register(typeof(TOwner), prop);
        return prop;
    }
}

public class Property<TValue> : Property
{
    public new TValue DefaultValue => (TValue)base.DefaultValue!;

    internal Property(string name, Type ownerType, TValue defaultValue, Action<PropertyObject, TValue>? onChanged = null)
        : base(name, typeof(TValue), ownerType, defaultValue, onChanged is null ? null : (obj, val) => onChanged(obj, (TValue)val!)) { }
}