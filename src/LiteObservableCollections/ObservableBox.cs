using LiteObservableCollections.ComponentModel;

namespace LiteObservableCollections;

/// <summary>
/// A small container that exposes a single observable property `Value`.
/// Useful when you need an observable wrapper for a single value.
/// </summary>
public class ObservableBox<T> : ObservableObject
{
    private T? _value;

    /// <summary>
    /// Initializes a new instance of the <see cref="ObservableBox{T}"/> class.
    /// </summary>
    public ObservableBox()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ObservableBox{T}"/> class with an initial value.
    /// </summary>
    public ObservableBox(T? initial)
    {
        _value = initial;
    }

    /// <summary>
    /// The single observable value contained in the box.
    /// Setting this property will raise PropertyChanging and PropertyChanged when the value changes.
    /// </summary>
    public T? Value
    {
        get => _value;
        set => SetProperty(ref _value, value);
    }

    /// <summary>
    /// Implicit conversion from ObservableBox<T> to T.
    /// Returns the boxed value or default(T) if null.
    /// </summary>
    public static implicit operator T?(ObservableBox<T>? box)
        => box is null ? default : box.Value;

    /// <summary>
    /// Implicit conversion from T to ObservableBox<T> (wraps the value).
    /// </summary>
    public static implicit operator ObservableBox<T>(T? value)
        => new(value);

    public override string? ToString() => _value?.ToString();
}
