using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace NFMWorld.Reactor;

/// <summary>
/// Static delegate for property change notifications. Uses a static method pointer
/// with an opaque context parameter to avoid per-instance delegate allocations.
/// </summary>
/// <typeparam name="T">The property value type.</typeparam>
/// <param name="context">Opaque context (typically the owning <see cref="Visual"/>).</param>
/// <param name="oldValue">The previous <see cref="Property{T}.ComputedValue"/>.</param>
/// <param name="newValue">The new <see cref="Property{T}.ComputedValue"/>.</param>
public delegate void PropertyChangedHandler<T>(object? context, T oldValue, T newValue);

/// <summary>
/// A reactive property with a three-tier priority chain: Override &gt; Style &gt; Default.
/// When <see cref="ComputedValue"/> changes, the optional <see cref="PropertyChangedHandler{T}"/>
/// is invoked with the owning context, old value, and new value.
/// </summary>
/// <typeparam name="T">The property value type.</typeparam>
public struct Property<T>
{
    private T _defaultValue;
    private T _styleValue;
    private T _overrideValue;
    private bool _hasStyleValue;
    private bool _hasOverrideValue;
    private object? _onChangedContext;
    private PropertyChangedHandler<T>? _onChanged;

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
        _hasStyleValue = false;
        _hasOverrideValue = false;
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
        get => _hasStyleValue ? _styleValue : default!;
    }

    /// <summary>
    /// Sets the style-sourced value. Call <see cref="ClearStyleValue"/> to clear.
    /// </summary>
    public void SetStyleValue(T value)
    {
        if (_hasStyleValue && EqualityComparer<T>.Default.Equals(_styleValue, value))
            return;
        var oldComputed = ComputedValue;
        _styleValue = value;
        _hasStyleValue = true;
        NotifyIfChanged(oldComputed);
    }

    /// <summary>
    /// Clears the style-sourced value, making this tier inactive.
    /// </summary>
    public void ClearStyleValue()
    {
        if (!_hasStyleValue) return;
        var oldComputed = ComputedValue;
        _hasStyleValue = false;
        _styleValue = default!;
        NotifyIfChanged(oldComputed);
    }

    /// <summary>
    /// Gets the explicit override value, or <c>default</c> if not set.
    /// </summary>
    public T OverrideValue
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _hasOverrideValue ? _overrideValue : default!;
    }

    /// <summary>
    /// Sets the explicit override value. Call <see cref="ClearOverrideValue"/> to clear.
    /// </summary>
    public void SetOverrideValue(T value)
    {
        if (_hasOverrideValue && EqualityComparer<T>.Default.Equals(_overrideValue, value))
            return;
        var oldComputed = ComputedValue;
        _overrideValue = value;
        _hasOverrideValue = true;
        NotifyIfChanged(oldComputed);
    }

    /// <summary>
    /// Clears the explicit override value, making this tier inactive.
    /// </summary>
    public void ClearOverrideValue()
    {
        if (!_hasOverrideValue) return;
        var oldComputed = ComputedValue;
        _hasOverrideValue = false;
        _overrideValue = default!;
        NotifyIfChanged(oldComputed);
    }

    /// <summary>
    /// The resolved value: OverrideValue ?? StyleValue ?? DefaultValue.
    /// </summary>
    public T ComputedValue
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _hasOverrideValue ? _overrideValue
             : _hasStyleValue ? _styleValue
             : _defaultValue;
    }

    /// <summary>
    /// Whether an explicit override is currently active.
    /// </summary>
    public bool HasOverride
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _hasOverrideValue;
    }

    /// <summary>
    /// Whether a style value is currently active.
    /// </summary>
    public bool HasStyle
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _hasStyleValue;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void NotifyIfChanged(T oldComputed)
    {
        var newComputed = ComputedValue;
        if (!EqualityComparer<T>.Default.Equals(oldComputed, newComputed))
            _onChanged?.Invoke(_onChangedContext, oldComputed, newComputed);
    }

    public override string ToString()
        => $"Property<{typeof(T).Name}>(Computed={ComputedValue}, Override={(_hasOverrideValue ? _overrideValue?.ToString() ?? "null" : "unset")}, Style={(_hasStyleValue ? _styleValue?.ToString() ?? "null" : "unset")}, Default={_defaultValue})";
}
