using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;

namespace LiteObservableCollections;

#nullable disable warnings

/// <summary>
/// A lite observable dictionary that supports <see cref="INotifyCollectionChanged"/> and <see cref="INotifyPropertyChanged"/>.
/// </summary>
public class ObservableDictionary<TKey, TValue> : IObservableDictionary<TKey, TValue>, INotifyCollectionChanged, INotifyPropertyChanged
{
    private readonly Dictionary<TKey, TValue> _dict;

    /// <summary>
    /// Initializes a new empty ObservableDictionary.
    /// </summary>
    public ObservableDictionary()
    {
        _dict = [];
    }

    /// <summary>
    /// Initializes a new ObservableDictionary with the specified Dictionary.
    /// </summary>
    /// <param name="dictionary">The Dictionary to initialize from.</param>
    public ObservableDictionary(Dictionary<TKey, TValue> dictionary)
    {
        _dict = dictionary ?? [];
    }

    /// <summary>
    /// Initializes a new ObservableDictionary with the specified collection.
    /// </summary>
    /// <param name="collection">The collection to initialize from.</param>
    public ObservableDictionary(IEnumerable<KeyValuePair<TKey, TValue>> collection)
    {
        _dict = [];
        if (collection != null)
        {
            foreach (var kv in collection)
                _dict.Add(kv.Key, kv.Value);
        }
    }

    /// <summary>
    /// Occurs when the dictionary changes.
    /// </summary>
    public event NotifyCollectionChangedEventHandler? CollectionChanged;

    /// <summary>
    /// Occurs when a property value changes.
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Gets or sets the value associated with the specified key.
    /// </summary>
    /// <param name="key">The key of the value to get or set.</param>
    /// <returns>The value associated with the specified key.</returns>
    public TValue this[TKey key]
    {
        get => _dict[key];
        set
        {
            bool exists = _dict.ContainsKey(key);
            TValue oldValue = exists ? _dict[key] : default!;
            _dict[key] = value;
            OnPropertyChanged("Item[]"); // Notify that the this[] has changed
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(
                exists ? NotifyCollectionChangedAction.Replace : NotifyCollectionChangedAction.Add,
                new KeyValuePair<TKey, TValue>(key, value),
                exists ? new KeyValuePair<TKey, TValue>(key, oldValue) : null,
                exists ? IndexOfKey(key) : -1));
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
    /// Gets the number of key/value pairs contained in the dictionary.
    /// </summary>
    public int Count => _dict.Count;

    /// <summary>
    /// Gets a value indicating whether the dictionary is read-only.
    /// </summary>
    public bool IsReadOnly => false;

    /// <summary>
    /// Adds the specified key and value to the dictionary.
    /// </summary>
    /// <param name="key">The key of the element to add.</param>
    /// <param name="value">The value of the element to add.</param>
    public void Add(TKey key, TValue value)
    {
        _dict.Add(key, value);
        OnPropertyChanged(nameof(Count));
        OnPropertyChanged("Item[]"); // Notify that the this[] has changed
        CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, new KeyValuePair<TKey, TValue>(key, value), _dict.Count - 1));
    }

    /// <summary>
    /// Removes the value with the specified key from the dictionary.
    /// </summary>
    /// <param name="key">The key of the element to remove.</param>
    /// <returns>true if the element is successfully found and removed; otherwise, false.</returns>
    public bool Remove(TKey key)
    {
        if (!_dict.TryGetValue(key, out TValue? value)) return false;
        int index = IndexOfKey(key);
        _dict.Remove(key);
        OnPropertyChanged(nameof(Count));
        OnPropertyChanged("Item[]"); // Notify that the this[] has changed
        CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, new KeyValuePair<TKey, TValue>(key, value), index));
        return true;
    }

    /// <summary>
    /// Removes all keys and values from the dictionary.
    /// </summary>
    public void Clear()
    {
        _dict.Clear();
        OnPropertyChanged(nameof(Count));
        OnPropertyChanged("Item[]"); // Notify that the this[] has changed
        CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }

    /// <summary>
    /// Determines whether the dictionary contains the specified key.
    /// </summary>
    /// <param name="key">The key to locate in the dictionary.</param>
    /// <returns>true if the dictionary contains an element with the specified key; otherwise, false.</returns>
    public bool ContainsKey(TKey key) => _dict.ContainsKey(key);

    /// <summary>
    /// Gets the value associated with the specified key.
    /// </summary>
    /// <param name="key">The key whose value to get.</param>
    /// <param name="value">When this method returns, contains the value associated with the specified key, if the key is found; otherwise, the default value for the type of the value parameter.</param>
    /// <returns>true if the dictionary contains an element with the specified key; otherwise, false.</returns>
    public bool TryGetValue(TKey key, out TValue value) => _dict.TryGetValue(key, out value);

    /// <summary>
    /// Adds the specified key/value pair to the dictionary.
    /// </summary>
    /// <param name="item">The key/value pair to add.</param>
    public void Add(KeyValuePair<TKey, TValue> item) => Add(item.Key, item.Value);

    /// <summary>
    /// Determines whether the dictionary contains the specified key/value pair.
    /// </summary>
    /// <param name="item">The key/value pair to locate in the dictionary.</param>
    /// <returns>true if the dictionary contains the specified key/value pair; otherwise, false.</returns>
    public bool Contains(KeyValuePair<TKey, TValue> item) => _dict.ContainsKey(item.Key) && EqualityComparer<TValue>.Default.Equals(_dict[item.Key], item.Value);

    /// <summary>
    /// Copies the elements of the dictionary to an array, starting at a particular array index.
    /// </summary>
    /// <param name="array">The destination array.</param>
    /// <param name="arrayIndex">The zero-based index in array at which copying begins.</param>
    public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) => ((IDictionary<TKey, TValue>)_dict).CopyTo(array, arrayIndex);

    /// <summary>
    /// Removes the specified key/value pair from the dictionary.
    /// </summary>
    /// <param name="item">The key/value pair to remove.</param>
    /// <returns>true if the key/value pair was removed; otherwise, false.</returns>
    public bool Remove(KeyValuePair<TKey, TValue> item) => Remove(item.Key);

    /// <summary>
    /// Returns an enumerator that iterates through the dictionary.
    /// </summary>
    /// <returns>An enumerator for the dictionary.</returns>
    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => _dict.GetEnumerator();

    /// <summary>
    /// Returns an enumerator that iterates through the dictionary.
    /// </summary>
    /// <returns>An enumerator for the dictionary.</returns>
    IEnumerator IEnumerable.GetEnumerator() => _dict.GetEnumerator();

    /// <summary>
    /// Gets the index of the specified key in the dictionary.
    /// </summary>
    /// <param name="key">The key to locate.</param>
    /// <returns>The index of the key if found; otherwise, -1.</returns>
    private int IndexOfKey(TKey key)
    {
        int i = 0;
        foreach (var k in _dict.Keys)
        {
            if (EqualityComparer<TKey>.Default.Equals(k, key)) return i;
            i++;
        }
        return -1;
    }

    /// <summary>
    /// Raises the <see cref="PropertyChanged"/> event.
    /// </summary>
    /// <param name="propertyName">The name of the property that changed.</param>
    private void OnPropertyChanged(string propertyName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

/// <summary>
/// Defines an observable dictionary interface.
/// </summary>
public interface IObservableDictionary<TKey, TValue> : IDictionary<TKey, TValue>, INotifyCollectionChanged, INotifyPropertyChanged;
