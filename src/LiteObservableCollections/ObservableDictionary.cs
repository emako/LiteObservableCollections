using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;

namespace LiteObservableCollections;

#nullable disable warnings

public class ObservableDictionary<TKey, TValue> : IObservableDictionary<TKey, TValue>, INotifyCollectionChanged, INotifyPropertyChanged
{
    private readonly Dictionary<TKey, TValue> _dict = [];

    public event NotifyCollectionChangedEventHandler? CollectionChanged;

    public event PropertyChangedEventHandler? PropertyChanged;

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

    public ICollection<TKey> Keys => _dict.Keys;
    public ICollection<TValue> Values => _dict.Values;
    public int Count => _dict.Count;
    public bool IsReadOnly => false;

    public void Add(TKey key, TValue value)
    {
        _dict.Add(key, value);
        OnPropertyChanged(nameof(Count));
        OnPropertyChanged("Item[]"); // Notify that the this[] has changed
        CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, new KeyValuePair<TKey, TValue>(key, value), _dict.Count - 1));
    }

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

    public void Clear()
    {
        _dict.Clear();
        OnPropertyChanged(nameof(Count));
        OnPropertyChanged("Item[]"); // Notify that the this[] has changed
        CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }

    public bool ContainsKey(TKey key) => _dict.ContainsKey(key);

    public bool TryGetValue(TKey key, out TValue value) => _dict.TryGetValue(key, out value);

    public void Add(KeyValuePair<TKey, TValue> item) => Add(item.Key, item.Value);

    public bool Contains(KeyValuePair<TKey, TValue> item) => _dict.ContainsKey(item.Key) && EqualityComparer<TValue>.Default.Equals(_dict[item.Key], item.Value);

    public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) => ((IDictionary<TKey, TValue>)_dict).CopyTo(array, arrayIndex);

    public bool Remove(KeyValuePair<TKey, TValue> item) => Remove(item.Key);

    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => _dict.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => _dict.GetEnumerator();

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

    private void OnPropertyChanged(string propertyName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

public interface IObservableDictionary<TKey, TValue> : IDictionary<TKey, TValue>, INotifyCollectionChanged, INotifyPropertyChanged
{
}
