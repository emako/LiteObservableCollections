using System.Collections.Concurrent;

namespace LiteObservableCollections.Concurrent;

/// <summary>
/// Thread-safe container for a single object, implemented using a <see cref="ConcurrentDictionary{TKey, TValue}"/> and
/// operating on a single entry keyed by a private byte value. This class provides simple atomic read/update
/// operations for storing one reference-type value across threads.
/// </summary>
/// <typeparam name="T">Type of the stored object. Must be a reference type.</typeparam>
public class ConcurrentObject<T> where T : class
{
    protected readonly ConcurrentDictionary<byte, T?> _dict = new();
    protected readonly byte _key = default;

    /// <summary>
    /// Gets or sets the stored object.
    /// </summary>
    /// <remarks>
    /// The getter returns the current value or <c>null</c> if no value is present. The setter replaces the stored
    /// value with the provided <paramref name="value"/>. Both operations are thread-safe.
    /// </remarks>
    public T? Value
    {
        get => TryGetValue();
        set => AddOrUpdate(value);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConcurrentObject{T}"/> class
    /// and sets the initial value.
    /// </summary>
    /// <param name="value">Initial value to store. May be <c>null</c> if callers intend to set a value later.</param>
    public ConcurrentObject(T value)
    {
        _dict[_key] = value;
    }

    /// <summary>
    /// Attempts to read the stored object in a thread-safe manner.
    /// </summary>
    /// <returns>The current stored value, or <c>null</c> if no value is present.</returns>
    public T? TryGetValue()
    {
        _dict.TryGetValue(_key, out T? value);
        return value;
    }

    /// <summary>
    /// Replaces the stored object with <paramref name="newValue"/> in a thread-safe manner.
    /// </summary>
    /// <param name="newValue">The new value to store; may be <c>null</c> to clear the stored value.</param>
    /// <remarks>
    /// This uses the ConcurrentDictionary indexer which performs an atomic write for the single internal key.
    /// </remarks>
    public void AddOrUpdate(T? newValue)
    {
        _dict[_key] = newValue;
    }

    /// <summary>
    /// Atomically invokes <paramref name="updater"/> to modify the stored object in-place.
    /// </summary>
    /// <param name="updater">An action that receives the current stored value and can modify its state.
    /// The action is invoked under the dictionary's atomic update callback. May be <c>null</c>, in which case
    /// no modification is performed.</param>
    /// <exception cref="InvalidOperationException">Thrown when the stored object does not exist (no value present).</exception>
    /// <remarks>
    /// This method uses <see cref="ConcurrentDictionary{TKey, TValue}.AddOrUpdate"/> with an add-factory that
    /// throws if the entry is missing. The updater is called with the current value (which may be <c>null</c>), and
    /// the same instance is returned so only side-effects applied by <paramref name="updater"/> persist.
    /// </remarks>
    public void Update(Action<T?> updater)
    {
        _dict.AddOrUpdate(_key,
            _ => throw new InvalidOperationException("Object does not exist"),
            (_, oldValue) =>
            {
                updater?.Invoke(oldValue);
                return oldValue;
            });
    }
}
