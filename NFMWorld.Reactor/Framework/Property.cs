using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace NFMWorld.Reactor;

/// <summary>
/// Static delegate for property change notifications. Uses a static method pointer
/// with an opaque context parameter to avoid per-instance delegate allocations.
/// </summary>
/// <typeparam name="T">The property value type.</typeparam>
/// <param name="context">Opaque context (typically the owning <see cref="Visual"/>).</param>
/// <param name="oldValue">The previous <see cref="Property{T}.ComputedValue"/>.</param>
/// <param name="newValue">The new <see cref="Property{T}.ComputedValue"/>.</param>
public delegate void PropertyChangedHandler<in T>(object? context, T oldValue, T newValue);

/// <summary>
/// A reactive property with a three-tier priority chain: Override &gt; Style &gt; Default.
/// When <see cref="ComputedValue"/> changes, the optional <see cref="PropertyChangedHandler{T}"/>
/// is invoked with the owning context, old value, and new value.
/// </summary>
/// <typeparam name="T">The property value type.</typeparam>
[StructLayout(LayoutKind.Auto)]
public struct Property<T>
{
    [Flags]
    private enum PropertyFlags
    {
        HasStyle = 1 << 0,
        HasOverride = 1 << 1,
    }
    
    private T _defaultValue;
    private T _styleValue;
    private T _overrideValue;
    private PropertyFlags _flags;
    private readonly object? _onChangedContext;
    private readonly PropertyChangedHandler<T>? _onChanged;

    /// <summary>
    /// Creates a new <see cref="Property{T}"/> with the given default value and optional
    /// change handler.
    /// </summary>
    /// <param name="defaultValue">The fallback value when neither style nor override is set.</param>
    /// <param name="onChangedContext">
    /// Opaque context passed to <paramref name="onChanged"/> (typically <c>this</c>).
    /// </param>
    /// <param name="onChanged">
    /// A static delegate invoked when <see cref="ComputedValue"/> changes.
    /// Use a static lambda to avoid per-instance allocations:
    /// <c>static (ctx, oldV, newV) => ((MyType)ctx!).OnPropChanged(oldV, newV)</c>
    /// </param>
    public Property(T defaultValue, object? onChangedContext = null, PropertyChangedHandler<T>? onChanged = null)
    {
        _defaultValue = defaultValue;
        _styleValue = default!;
        _overrideValue = default!;
        HasStyle = false;
        HasOverride = false;
        _onChangedContext = onChangedContext;
        _onChanged = onChanged;
    }

    /// <summary>
    /// Implicit conversion from <typeparamref name="T"/> creates a <see cref="Property{T}"/>
    /// with the given default value and no change handler.
    /// </summary>
    public static implicit operator Property<T>(T defaultValue) => new(defaultValue);

    /// <summary>
    /// The fallback value used when neither <see cref="StyleValue"/> nor
    /// <see cref="OverrideValue"/> is set.
    /// </summary>
    public T DefaultValue
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _defaultValue;
        set
        {
            if (EqualityComparer<T>.Default.Equals(_defaultValue, value))
                return;
            var oldComputed = ComputedValue;
            _defaultValue = value;
            NotifyIfChanged(oldComputed);
        }
    }

    /// <summary>
    /// Gets the style-sourced value, or <c>default</c> if not set.
    /// </summary>
    public T StyleValue
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => HasStyle ? _styleValue : default!;
    }

    /// <summary>
    /// Sets the style-sourced value. Call <see cref="ClearStyleValue"/> to clear.
    /// </summary>
    public void SetStyleValue(T value)
    {
        if (HasStyle && EqualityComparer<T>.Default.Equals(_styleValue, value))
            return;
        var oldComputed = ComputedValue;
        _styleValue = value;
        HasStyle = true;
        NotifyIfChanged(oldComputed);
    }

    /// <summary>
    /// Clears the style-sourced value, making this tier inactive.
    /// </summary>
    public void ClearStyleValue()
    {
        if (!HasStyle) return;
        var oldComputed = ComputedValue;
        HasStyle = false;
        _styleValue = default!;
        NotifyIfChanged(oldComputed);
    }

    /// <summary>
    /// Gets the explicit override value, or <c>default</c> if not set.
    /// </summary>
    public T OverrideValue
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => HasOverride ? _overrideValue : default!;
    }

    /// <summary>
    /// Sets the explicit override value. Call <see cref="ClearOverrideValue"/> to clear.
    /// </summary>
    public void SetOverrideValue(T value)
    {
        if (HasOverride && EqualityComparer<T>.Default.Equals(_overrideValue, value))
            return;
        var oldComputed = ComputedValue;
        _overrideValue = value;
        HasOverride = true;
        NotifyIfChanged(oldComputed);
    }

    /// <summary>
    /// Clears the explicit override value, making this tier inactive.
    /// </summary>
    public void ClearOverrideValue()
    {
        if (!HasOverride) return;
        var oldComputed = ComputedValue;
        HasOverride = false;
        _overrideValue = default!;
        NotifyIfChanged(oldComputed);
    }

    /// <summary>
    /// The resolved value: OverrideValue ?? StyleValue ?? DefaultValue.
    /// </summary>
    public T ComputedValue
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => HasOverride ? _overrideValue
             : HasStyle ? _styleValue
             : _defaultValue;
    }

    /// <summary>
    /// Whether an explicit override is currently active.
    /// </summary>
    public bool HasOverride
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _flags.HasFlag(PropertyFlags.HasOverride);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private set
        {
            if (value)
                _flags |= PropertyFlags.HasOverride;
            else
                _flags &= ~PropertyFlags.HasOverride;
        }
    }

    /// <summary>
    /// Whether a style value is currently active.
    /// </summary>
    public bool HasStyle
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _flags.HasFlag(PropertyFlags.HasStyle);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private set
        {
            if (value)
                _flags |= PropertyFlags.HasStyle;
            else
                _flags &= ~PropertyFlags.HasStyle;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void NotifyIfChanged(T oldComputed)
    {
        var newComputed = ComputedValue;
        if (!EqualityComparer<T>.Default.Equals(oldComputed, newComputed))
            _onChanged?.Invoke(_onChangedContext, oldComputed, newComputed);
    }

    public override string ToString()
        => $"Property<{typeof(T).Name}>(Computed={ComputedValue}, Override={(HasOverride ? _overrideValue?.ToString() ?? "null" : "unset")}, Style={(HasStyle ? _styleValue?.ToString() ?? "null" : "unset")}, Default={_defaultValue})";
}
