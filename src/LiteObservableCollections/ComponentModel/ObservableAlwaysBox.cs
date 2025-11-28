namespace LiteObservableCollections.ComponentModel;

/// <summary>
/// A small container that exposes a single observable property <see cref="Value"/>.
/// Useful when you need an observable wrapper for a single value.
/// Supports optional force-refresh: notify observers even if the value did not change.
/// </summary>
/// <typeparam name="T">The type of the contained value.</typeparam>
public class ObservableAlwaysBox<T> : ObservableAlwaysObject
{
    private T? _value;

    /// <summary>
    /// Initializes a new instance of the <see cref="ObservableAlwaysBox{T}"/> class.
    /// </summary>
    public ObservableAlwaysBox()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ObservableAlwaysBox{T}"/> class with an initial value.
    /// </summary>
    /// <param name="initial">The initial value to wrap.</param>
    public ObservableAlwaysBox(T? initial)
    {
        _value = initial;
    }

    /// <summary>
    /// The single observable value contained in the box.
    /// Setting this property will raise PropertyChanging and PropertyChanged events when the value changes.
    /// </summary>
    public T? Value
    {
        get => _value;
        set => SetPropertyAlways(ref _value, value);
    }

    /// <summary>
    /// Implicit conversion from ObservableAlwaysBox to T.
    /// Returns the boxed value or default(T) if null.
    /// </summary>
    public static implicit operator T?(ObservableAlwaysBox<T>? box)
        => box is null ? default : box.Value;

    /// <summary>
    /// Implicit conversion from T to ObservableAlwaysBox (wraps the value).
    /// </summary>
    public static implicit operator ObservableAlwaysBox<T>(T? value)
        => new(value);

    /// <summary>
    /// Returns the string representation of the contained value.
    /// </summary>
    public override string? ToString() => _value?.ToString();
}
