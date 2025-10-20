using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Specialized;
using System.ComponentModel;

namespace LiteObservableCollections.Concurrent;

/// <summary>
/// A thread-safe observable dictionary built on top of <see cref="ConcurrentDictionary{TKey, TValue}"/>.
/// This class exposes a dictionary-like API and raises <see cref="INotifyCollectionChanged"/> and
/// <see cref="INotifyPropertyChanged"/> events when the collection is modified.
///
/// Notes:
/// - The underlying storage is a <see cref="ConcurrentDictionary{TKey, TValue}"/>, so concurrent reads and
///   writes are safe. Event handlers are invoked after the mutation occurs; subscribers may observe
///   concurrent changes between the mutation and handler execution.
/// - Some notifications (for example Replace vs Add) are determined using non-atomic checks. If you require
///   strictly atomic semantics for notifications, consider using explicit atomic operations and tailoring
///   the event behavior accordingly.
/// </summary>
/// <typeparam name="TKey">Type of the keys.</typeparam>
/// <typeparam name="TValue">Type of the values.</typeparam>
public class ObservableConcurrentDictionary<TKey, TValue> : IDictionary<TKey, TValue>, INotifyCollectionChanged, INotifyPropertyChanged where TKey : notnull
{
    private const string IndexerName = "Item[]";
    private readonly ConcurrentDictionary<TKey, TValue> _dict;

    /// <summary>
    /// Initializes a new empty <see cref="ObservableConcurrentDictionary{TKey, TValue}"/>.
    /// </summary>
    public ObservableConcurrentDictionary()
    {
        _dict = new ConcurrentDictionary<TKey, TValue>();
    }

    /// <summary>
    /// Initializes a new <see cref="ObservableConcurrentDictionary{TKey, TValue}"/> containing the elements
    /// copied from the specified <paramref name="dictionary"/>.
    /// </summary>
    /// <param name="dictionary">The dictionary whose elements are copied. If <c>null</c>, an empty dictionary is created.</param>
    public ObservableConcurrentDictionary(IDictionary<TKey, TValue> dictionary)
    {
        _dict = dictionary != null ? new ConcurrentDictionary<TKey, TValue>(dictionary) : new ConcurrentDictionary<TKey, TValue>();
    }

    /// <summary>
    /// Occurs when the dictionary content changes (items added, removed, replaced or the collection reset).
    /// </summary>
    public event NotifyCollectionChangedEventHandler? CollectionChanged;

    /// <summary>
    /// Occurs when a property on the collection changes (for example, <see cref="Count"/> or the indexer).
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Gets or sets the value associated with the specified key. Setting a value will add or replace the entry
    /// and raise collection/property change notifications.
    /// </summary>
    /// <exception cref="KeyNotFoundException">The getter throws when the key does not exist.</exception>
    public TValue this[TKey key]
    {
        get => _dict[key];
        set
        {
            // Note: ContainsKey followed by indexer is not atomic. This determines whether to raise Add or Replace.
            bool exists = _dict.ContainsKey(key);
            TValue old = exists ? _dict[key] : default!;
            _dict[key] = value;
            OnPropertyChanged(IndexerName);
            OnPropertyChanged(nameof(Count));
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(
                exists ? NotifyCollectionChangedAction.Replace : NotifyCollectionChangedAction.Add,
                new KeyValuePair<TKey, TValue>(key, value),
                exists ? new KeyValuePair<TKey, TValue>(key, old) : null));
        }
    }

    /// <summary>
    /// Gets a collection containing the keys in the dictionary.
    /// </summary>
    public ICollection<TKey> Keys => _dict.Keys;

    /// <summary>
    /// Gets a collection containing the values in the dictionary.
    /// </summary>
    public ICollection<TValue> Values => _dict.Values;

    /// <summary>
    /// Gets the number of elements contained in the dictionary.
    /// </summary>
    public int Count => _dict.Count;

    /// <summary>
    /// Gets a value indicating whether the dictionary is read-only. Always <c>false</c> for this implementation.
    /// </summary>
    public bool IsReadOnly => false;

    /// <summary>
    /// Adds the specified key and value to the dictionary.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when an element with the same key already exists.</exception>
    public void Add(TKey key, TValue value)
    {
        if (_dict.TryAdd(key, value))
        {
            OnPropertyChanged(nameof(Count));
            OnPropertyChanged(IndexerName);
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, new KeyValuePair<TKey, TValue>(key, value)));
        }
        else
        {
            throw new ArgumentException("An item with the same key has already been added.", nameof(key));
        }
    }

    /// <summary>
    /// Removes the value with the specified key from the dictionary.
    /// </summary>
    /// <returns><c>true</c> if the element is successfully removed; otherwise, <c>false</c>.</returns>
    public bool Remove(TKey key)
    {
        if (_dict.TryRemove(key, out var value))
        {
            OnPropertyChanged(nameof(Count));
            OnPropertyChanged(IndexerName);
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, new KeyValuePair<TKey, TValue>(key, value)));
            return true;
        }
        return false;
    }

    /// <summary>
    /// Removes all keys and values from the dictionary.
    /// </summary>
    public void Clear()
    {
        _dict.Clear();
        OnPropertyChanged(nameof(Count));
        OnPropertyChanged(IndexerName);
        CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }

    /// <summary>
    /// Determines whether the dictionary contains the specified key.
    /// </summary>
    public bool ContainsKey(TKey key) => _dict.ContainsKey(key);

    /// <summary>
    /// Gets the value associated with the specified key.
    /// </summary>
    /// <returns><c>true</c> if the key was found; otherwise, <c>false</c>.</returns>
    public bool TryGetValue(TKey key, out TValue value) => _dict.TryGetValue(key, out value);

    /// <summary>
    /// Adds the specified key/value pair to the dictionary.
    /// </summary>
    public void Add(KeyValuePair<TKey, TValue> item) => Add(item.Key, item.Value);

    /// <summary>
    /// Determines whether the dictionary contains the specified key/value pair.
    /// </summary>
    public bool Contains(KeyValuePair<TKey, TValue> item)
        => _dict.TryGetValue(item.Key, out var v) && EqualityComparer<TValue>.Default.Equals(v, item.Value);

    /// <summary>
    /// Copies the elements of the dictionary to an array, starting at a particular array index.
    /// </summary>
    public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        => ((ICollection<KeyValuePair<TKey, TValue>>)_dict).CopyTo(array, arrayIndex);

    /// <summary>
    /// Removes the specified key/value pair from the dictionary.
    /// </summary>
    public bool Remove(KeyValuePair<TKey, TValue> item) => Remove(item.Key);

    /// <summary>
    /// Returns an enumerator that iterates through the dictionary. Enumeration represents a snapshot view
    /// of the dictionary that may reflect concurrent modifications.
    /// </summary>
    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => _dict.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => _dict.GetEnumerator();

    /// <summary>
    /// Raises the <see cref="PropertyChanged"/> event for the specified property name.
    /// </summary>
    /// <param name="propertyName">Name of the property that changed.</param>
    private void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
