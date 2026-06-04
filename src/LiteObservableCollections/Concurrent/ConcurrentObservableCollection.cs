using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;

namespace LiteObservableCollections.Concurrent;

/// <summary>
/// A thread-safe observable collection. This implementation uses an internal <see cref="List{T}"/>
/// guarded by a private lock to provide simple thread-safety for collection operations and to raise
/// <see cref="INotifyCollectionChanged"/> and <see cref="INotifyPropertyChanged"/> events.
///
/// Remarks:
/// - Enumeration returns a snapshot (array) to avoid holding the lock while consumers iterate.
/// - Events are raised after the mutation completes and the lock is released; subscribers may
///   observe further concurrent modifications between the mutation and the event handling.
/// </summary>
/// <typeparam name="T">Type of items contained in the collection.</typeparam>
public class ConcurrentObservableCollection<T> : IList<T>, ICollection, INotifyCollectionChanged, INotifyPropertyChanged
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
    /// Gets or sets whether change notifications (CollectionChanged and PropertyChanged) are raised.
    /// When false, modifications do not raise any events. Default is true.
    /// </summary>
    public bool IsNotifyEnabled { get; set; } = true;

    /// <summary>
    /// Occurs when the collection changes.
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
            RaiseCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, value, old, index));
        }
    }

    /// <summary>
    /// Gets the number of elements contained in the collection.
    /// </summary>
    public int Count
    {
        get { lock (_sync) return _items.Count; }
    }

    /// <summary>
    /// Gets a value indicating whether the collection is read-only. Always false for this implementation.
    /// </summary>
    public bool IsReadOnly => false;

    /// <summary>
    /// Adds an item to the collection and raises notifications.
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
        RaiseCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, index));
    }

    /// <summary>
    /// Adds the elements of the specified collection to the end of the collection.
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
        RaiseCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }

    /// <summary>
    /// Adds multiple items to the collection (convenience overload).
    /// </summary>
    public void AddRange(params T[] items)
    {
        AddRange((IEnumerable<T>)items);
    }

    /// <summary>
    /// Inserts the elements of the specified collection into the collection at the specified index.
    /// </summary>
    public void InsertRange(int index, IEnumerable<T> items)
    {
        if (items == null) return;
        if (IsNotifyOnEachInRange)
        {
            var i = index;
            foreach (var item in items)
            {
                Insert(i, item);
                i++;
            }
            return;
        }

        lock (_sync)
        {
            _items.InsertRange(index, items);
        }
        OnPropertyChanged(nameof(Count));
        OnPropertyChanged(IndexerName);
        RaiseCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }

    /// <summary>
    /// Removes the first occurrence of a specific object from the collection.
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
        RaiseCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item, index));
        return true;
    }

    /// <summary>
    /// Removes the elements in the specified collection from the collection.
    /// </summary>
    public void RemoveRange(IEnumerable<T> items)
    {
        if (items == null) return;
        if (IsNotifyOnEachInRange)
        {
            foreach (var item in items) Remove(item);
            return;
        }

        lock (_sync)
        {
            foreach (var item in items)
            {
                _items.Remove(item);
            }
        }
        OnPropertyChanged(nameof(Count));
        OnPropertyChanged(IndexerName);
        RaiseCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }

    /// <summary>
    /// Removes all items from the collection.
    /// </summary>
    public void Clear()
    {
        lock (_sync) { _items.Clear(); }
        OnPropertyChanged(nameof(Count));
        OnPropertyChanged(IndexerName);
        RaiseCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }

    /// <summary>
    /// Resets the collection to the specified items: clears the collection, adds all items from the given enumeration,
    /// and raises a single <see cref="CollectionChanged"/> with <see cref="NotifyCollectionChangedAction.Reset"/>.
    /// </summary>
    /// <param name="items">The items to set. If null, the collection is cleared.</param>
    public void Reset(IEnumerable<T>? items)
    {
        lock (_sync)
        {
            _items.Clear();
            if (items != null)
                _items.AddRange(items);
        }
        OnPropertyChanged(nameof(Count));
        OnPropertyChanged(IndexerName);
        RaiseCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }

    /// <summary>
    /// Determines whether the collection contains a specific value.
    /// </summary>
    public bool Contains(T item) { lock (_sync) return _items.Contains(item); }

    /// <summary>
    /// Copies the elements of the collection to an array, starting at a particular array index.
    /// </summary>
    public void CopyTo(T[] array, int arrayIndex) { lock (_sync) _items.CopyTo(array, arrayIndex); }

    /// <summary>
    /// Returns an enumerator that iterates through the collection. The enumerator is backed by a snapshot array.
    /// </summary>
    public IEnumerator<T> GetEnumerator()
    {
        T[] snapshot;
        lock (_sync) { snapshot = [.. _items]; }
        return ((IEnumerable<T>)snapshot).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>
    /// Determines the index of a specific item in the collection.
    /// </summary>
    public int IndexOf(T item) { lock (_sync) return _items.IndexOf(item); }

    /// <summary>
    /// Inserts an item to the collection at the specified index.
    /// </summary>
    public void Insert(int index, T item)
    {
        lock (_sync) { _items.Insert(index, item); }
        OnPropertyChanged(nameof(Count));
        OnPropertyChanged(IndexerName);
        RaiseCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, index));
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
        RaiseCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, old, index));
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
        RaiseCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Move, item, newIndex, oldIndex));
    }

    /// <summary>
    /// Sorts the elements in the collection using the default comparer.
    /// </summary>
    public void Sort()
    {
        Sort(comparer: null);
    }

    /// <summary>
    /// Sorts the elements in the collection using the specified comparer.
    /// </summary>
    /// <param name="comparer">The comparer to use when comparing elements.</param>
    public void Sort(IComparer<T>? comparer)
    {
        lock (_sync)
        {
            _items.Sort(comparer);
        }
        OnPropertyChanged(IndexerName);
        RaiseCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }

    /// <summary>
    /// Performs a stable sort on the collection using the default comparer.
    /// </summary>
    public void StableSort()
    {
        StableSort(comparer: null);
    }

    /// <summary>
    /// Performs a stable sort on the collection using the specified comparer.
    /// </summary>
    /// <param name="comparer">The comparer to use when comparing elements.</param>
    public void StableSort(IComparer<T>? comparer)
    {
        lock (_sync)
        {
#if NET7_0_OR_GREATER
            var sorted = _items.Order(comparer).ToList();
#else
            var sorted = _items.OrderBy(x => x, comparer).ToList();
#endif
            _items.Clear();
            _items.AddRange(sorted);
        }
        OnPropertyChanged(IndexerName);
        RaiseCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
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

    private void OnPropertyChanged(string propertyName) =>
        RaisePropertyChanged(new PropertyChangedEventArgs(propertyName));

    private void RaisePropertyChanged(PropertyChangedEventArgs e)
    {
        if (!IsNotifyEnabled || PropertyChanged == null) return;
        PropertyChanged.Invoke(this, e);
    }

    private void RaiseCollectionChanged(NotifyCollectionChangedEventArgs e)
    {
        if (!IsNotifyEnabled || CollectionChanged == null) return;
        CollectionChanged.Invoke(this, e);
    }
}
