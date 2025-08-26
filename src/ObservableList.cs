using System.Collections.Specialized;
using System.ComponentModel;
using System.Collections;

namespace LiteObservableCollections;

/// <summary>
/// A lite observable list that supports INotifyCollectionChanged
/// </summary>
public partial class ObservableList<T> : IObservableList<T>, INotifyCollectionChanged, INotifyPropertyChanged
{
    private readonly List<T> _items = [];

    public event NotifyCollectionChangedEventHandler? CollectionChanged;

    public event PropertyChangedEventHandler? PropertyChanged;

    public T this[int index]
    {
        get => _items[index];
        set
        {
            T oldItem = _items[index];
            _items[index] = value;
            OnPropertyChanged("Item[]"); // Notify that the this[] has changed
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, value, oldItem, index));
        }
    }

    public int Count => _items.Count;
    public bool IsReadOnly => false;

    public void Add(T item)
    {
        _items.Add(item);
        OnPropertyChanged(nameof(Count));
        OnPropertyChanged("Item[]"); // Notify that the this[] has changed
        CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, _items.Count - 1));
    }

    public void AddRange(IEnumerable<T> items)
    {
        if (items == null) return;
        var itemsToAdd = items is ICollection<T> col ? [.. col] : items.ToList();
        if (itemsToAdd.Count == 0) return;
        int startIndex = _items.Count;
        foreach (T item in itemsToAdd)
            _items.Add(item);

        OnPropertyChanged(nameof(Count));
        OnPropertyChanged("Item[]"); // Notify that the this[] has changed
        CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset, itemsToAdd, startIndex));
    }

    public bool Remove(T item)
    {
        int index = _items.IndexOf(item);
        if (index < 0) return false;

        _items.RemoveAt(index);
        OnPropertyChanged(nameof(Count));
        OnPropertyChanged("Item[]"); // Notify that the this[] has changed
        CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item, index));
        return true;
    }

    public void Clear()
    {
        _items.Clear();
        OnPropertyChanged(nameof(Count));
        OnPropertyChanged("Item[]"); // Notify that the this[] has changed
        CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }

    public bool Contains(T item) => _items.Contains(item);

    public void CopyTo(T[] array, int arrayIndex) => _items.CopyTo(array, arrayIndex);

    public IEnumerator<T> GetEnumerator() => _items.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => _items.GetEnumerator();

    public int IndexOf(T item) => _items.IndexOf(item);

    public void Insert(int index, T item)
    {
        _items.Insert(index, item);
        OnPropertyChanged(nameof(Count));
        OnPropertyChanged("Item[]");
        CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, index));
    }

    public void RemoveAt(int index)
    {
        T oldItem = _items[index];
        _items.RemoveAt(index);
        OnPropertyChanged(nameof(Count));
        OnPropertyChanged("Item[]");
        CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, oldItem, index));
    }

    private void OnPropertyChanged(string propertyName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

public interface IObservableList<T> : IList<T>, INotifyCollectionChanged, INotifyPropertyChanged
{
    public void AddRange(IEnumerable<T> items);
}
