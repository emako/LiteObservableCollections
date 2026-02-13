using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;

namespace LiteObservableCollections.Concurrent;

/// <summary>
/// A thread-safe observable list. This implementation uses an internal <see cref="List{T}"/>
/// guarded by a private lock to provide simple thread-safety for list operations and to raise
/// <see cref="INotifyCollectionChanged"/> and <see cref="INotifyPropertyChanged"/> events.
///
/// Remarks:
/// - Enumeration returns a snapshot (array) to avoid holding the lock while consumers iterate.
/// - Events are raised after the mutation completes and the lock is released; subscribers may
///   observe further concurrent modifications between the mutation and the event handling.
/// </summary>
/// <typeparam name="T">Type of items contained in the list.</typeparam>
public class ObservableConcurrentList<T> : IList<T>, ICollection, INotifyCollectionChanged, INotifyPropertyChanged
{
    private const string IndexerName = "Item[]";
    private readonly List<T> _items = [];
    private readonly object _sync = new();

    /// <summary>
    /// If true, AddRange/RemoveRange will raise an add/remove notification for each item in the range.
    /// Otherwise a single Reset notification will be raised. Default is false.
    /// </summary>
    public bool IsNotifyOnEachInRange { get; set; } = false;

    /// <summary>
    /// Occurs when the list changes.
    /// </summary>
    public event NotifyCollectionChangedEventHandler? CollectionChanged;

    /// <summary>
    /// Occurs when a property value changes.
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Gets or sets the element at the specified index.
    /// </summary>
    public T this[int index]
    {
        get
        {
            lock (_sync) { return _items[index]; }
        }
        set
        {
            T old;
            lock (_sync)
            {
                old = _items[index];
                _items[index] = value;
            }
            OnPropertyChanged(IndexerName);
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, value, old, index));
        }
    }

    /// <summary>
    /// Gets the number of elements contained in the list.
    /// </summary>
    public int Count
    {
        get { lock (_sync) return _items.Count; }
    }

    /// <summary>
    /// Gets a value indicating whether the list is read-only. Always false for this implementation.
    /// </summary>
    public bool IsReadOnly => false;

    /// <summary>
    /// Adds an item to the list and raises notifications.
    /// </summary>
    public void Add(T item)
    {
        int index;
        lock (_sync)
        {
            _items.Add(item);
            index = _items.Count - 1;
        }
        OnPropertyChanged(nameof(Count));
        OnPropertyChanged(IndexerName);
        CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, index));
    }

    /// <summary>
    /// Adds the elements of the specified collection to the end of the list.
    /// </summary>
    public void AddRange(IEnumerable<T> items)
    {
        if (items == null) return;
        if (IsNotifyOnEachInRange)
        {
            foreach (var item in items) Add(item);
            return;
        }

        lock (_sync)
        {
            _items.AddRange(items);
        }
        OnPropertyChanged(nameof(Count));
        OnPropertyChanged(IndexerName);
        CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }

    /// <summary>
    /// Removes the first occurrence of a specific object from the list.
    /// </summary>
    public bool Remove(T item)
    {
        int index;
        lock (_sync)
        {
            index = _items.IndexOf(item);
            if (index < 0) return false;
            _items.RemoveAt(index);
        }
        OnPropertyChanged(nameof(Count));
        OnPropertyChanged(IndexerName);
        CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item, index));
        return true;
    }

    /// <summary>
    /// Removes all items from the list.
    /// </summary>
    public void Clear()
    {
        lock (_sync) { _items.Clear(); }
        OnPropertyChanged(nameof(Count));
        OnPropertyChanged(IndexerName);
        CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }

    /// <summary>
    /// Determines whether the list contains a specific value.
    /// </summary>
    public bool Contains(T item) { lock (_sync) return _items.Contains(item); }

    /// <summary>
    /// Copies the elements of the list to an array, starting at a particular array index.
    /// </summary>
    public void CopyTo(T[] array, int arrayIndex) { lock (_sync) _items.CopyTo(array, arrayIndex); }

    /// <summary>
    /// Returns an enumerator that iterates through the list. The enumerator is backed by a snapshot array.
    /// </summary>
    public IEnumerator<T> GetEnumerator()
    {
        T[] snapshot;
        lock (_sync) { snapshot = _items.ToArray(); }
        return ((IEnumerable<T>)snapshot).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>
    /// Determines the index of a specific item in the list.
    /// </summary>
    public int IndexOf(T item) { lock (_sync) return _items.IndexOf(item); }

    /// <summary>
    /// Inserts an item to the list at the specified index.
    /// </summary>
    public void Insert(int index, T item)
    {
        lock (_sync) { _items.Insert(index, item); }
        OnPropertyChanged(nameof(Count));
        OnPropertyChanged(IndexerName);
        CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, index));
    }

    /// <summary>
    /// Removes the item at the specified index.
    /// </summary>
    public void RemoveAt(int index)
    {
        T old;
        lock (_sync)
        {
            old = _items[index];
            _items.RemoveAt(index);
        }
        OnPropertyChanged(nameof(Count));
        OnPropertyChanged(IndexerName);
        CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, old, index));
    }

    /// <summary>
    /// Moves the element at the specified old index to the new index and raises collection change notifications.
    /// </summary>
    public void Move(int oldIndex, int newIndex)
    {
        T item;
        lock (_sync)
        {
            if (oldIndex < 0 || oldIndex >= _items.Count) throw new ArgumentOutOfRangeException(nameof(oldIndex));
            if (newIndex < 0 || newIndex >= _items.Count) throw new ArgumentOutOfRangeException(nameof(newIndex));
            if (oldIndex == newIndex) return;
            item = _items[oldIndex];
            _items.RemoveAt(oldIndex);
            _items.Insert(newIndex, item);
        }
        OnPropertyChanged(IndexerName);
        CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Move, item, newIndex, oldIndex));
    }

    /// <summary>
    /// Copies the elements of the collection to an <see cref="Array"/>, starting at a particular index.
    /// </summary>
    void ICollection.CopyTo(Array array, int index)
    {
        if (array is T[] tArray) CopyTo(tArray, index);
        else
        {
            foreach (var item in this) array.SetValue(item, index++);
        }
    }

    object ICollection.SyncRoot => ((ICollection)_items).SyncRoot!;
    bool ICollection.IsSynchronized => false;
    int ICollection.Count => Count;

    /// <summary>
    /// Raises the <see cref="PropertyChanged"/> event for the specified property name.
    /// </summary>
    private void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
