using System.ComponentModel;

namespace LiteObservableCollections.Concurrent.ComponentModel;

/// <summary>
/// An observable wrapper around <see cref="ConcurrentObject{T}"/> which raises
/// <see cref="INotifyPropertyChanged.PropertyChanged"/> when the stored value changes.
/// </summary>
/// <typeparam name="T">Reference type stored by the concurrent object.</typeparam>
public class ObservableConcurrentObject<T> : INotifyPropertyChanged where T : class
{
    private readonly ConcurrentObject<T> _inner;
    private const string ValuePropertyName = nameof(Value);

    /// <summary>
    /// Raised when the value changes.
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Creates a new observable concurrent object with an initial value.
    /// </summary>
    /// <param name="value">Initial stored value; may be null.</param>
    public ObservableConcurrentObject(T? value)
    {
        _inner = new ConcurrentObject<T>(value!);
    }

    /// <summary>
    /// Gets or sets the stored value. Setting the value will replace the inner value and raise
    /// <see cref="PropertyChanged"/> for <c>Value</c>.
    /// </summary>
    public T? Value
    {
        get => _inner.TryGetValue();
        set
        {
            _inner.AddOrUpdate(value);
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(ValuePropertyName));
        }
    }

    /// <summary>
    /// Atomically updates the stored object in-place by invoking the supplied updater under
    /// the concurrent object's atomic update callback. After update, <see cref="PropertyChanged"/>
    /// is raised for <c>Value</c>.
    /// </summary>
    /// <param name="updater">Action that receives the current value and may modify it.</param>
    public void Update(Action<T?> updater)
    {
        _inner.Update(updater);
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(ValuePropertyName));
    }

    /// <summary>
    /// Raises <see cref="PropertyChanged"/> for a property name (useful when inner value mutates its state without replacement).
    /// </summary>
    public void RaisePropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
