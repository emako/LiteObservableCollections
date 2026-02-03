using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace LiteObservableCollections;

/// <summary>
/// A lite observable collection that supports <see cref="INotifyCollectionChanged"/> and <see cref="INotifyPropertyChanged"/>, and supports AddRange for batch addition.
/// </summary>
/// <typeparam name="T">The type of elements in the collection.</typeparam>
public class ObservableCollection<T> : IObservableCollection<T>, INotifyCollectionChanged, INotifyPropertyChanged
{
    /// <summary>
    /// Indexer Name to notify that the this[] has changed.
    /// </summary>
    private const string IndexerName = "Item[]";

    /// <summary>
    /// The internal list storing the collection elements.
    /// </summary>
    private readonly List<T> _items;

    /// <summary>
    /// Indicates the AddRange or RemoveRange notification behavior.
    /// If true, AddRange or RemoveRange will trigger CollectionChanged with `NotifyCollectionChangedAction.Add` or `NotifyCollectionChangedAction.Remove` for each item added or removed;
    /// otherwise, it will only trigger once at the end with `NotifyCollectionChangedAction.Reset`.
    /// This allows for more granular change notifications, enabling UI scenarios such as per-item insertion or deletion animations.
    /// </summary>
    public bool IsNotifyOnEachInRange { get; set; } = false;

    /// <summary>
    /// Initializes a new empty ObservableCollection.
    /// </summary>
    public ObservableCollection()
    {
        _items = [];
    }

    /// <summary>
    /// Initializes a new ObservableCollection with the specified collection.
    /// </summary>
    /// <param name="collection">The collection to initialize from.</param>
    public ObservableCollection(IEnumerable<T> collection)
    {
        _items = collection != null ? [.. collection] : [];
    }

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
    /// <param name="index">The zero-based index of the element to get or set.</param>
    /// <returns>The element at the specified index.</returns>
    public T this[int index]
    {
        get => _items[index];
        set
        {
            T oldItem = _items[index];
            _items[index] = value;
            OnPropertyChanged(IndexerName);
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, value, oldItem, index));
        }
    }

    /// <summary>
    /// Gets the number of elements contained in the collection.
    /// </summary>
    public int Count => _items.Count;

    /// <summary>
    /// Gets a value indicating whether the collection is read-only.
    /// </summary>
    public bool IsReadOnly => false;

    /// <summary>
    /// Gets a value indicating whether the collection has a fixed size.
    /// </summary>
    public bool IsFixedSize => false;

    /// <summary>
    /// Gets an object that can be used to synchronize access to the collection.
    /// </summary>
    public object SyncRoot => ((ICollection)_items).SyncRoot;

    /// <summary>
    /// Gets a value indicating whether access to the collection is synchronized (thread safe).
    /// </summary>
    public bool IsSynchronized => ((ICollection)_items).IsSynchronized;

    /// <summary>
    /// Adds an item to the end of the collection.
    /// </summary>
    /// <param name="item">The item to add.</param>
    public void Add(T item)
    {
        _items.Add(item);
        OnPropertyChanged(nameof(Count));
        OnPropertyChanged(IndexerName);
        CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, _items.Count - 1));
    }

    /// <summary>
    /// Adds the elements of the specified collection to the end of the collection.
    /// </summary>
    /// <param name="items">The collection whose elements should be added.</param>
    public void AddRange(IEnumerable<T> items)
    {
        if (items == null) return;
        if (IsNotifyOnEachInRange)
        {
            foreach (var item in items)
                Add(item);
        }
        else
        {
            _items.AddRange(items);
            OnPropertyChanged(nameof(Count));
            OnPropertyChanged(IndexerName);
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }
    }

    /// <summary>
    /// Removes the first occurrence of each item from the specified collection.
    /// </summary>
    /// <param name="items">The collection whose elements should be removed from the collection.</param>
    public void RemoveRange(IEnumerable<T> items)
    {
        if (items == null) return;
        if (IsNotifyOnEachInRange)
        {
            foreach (var item in items)
                Remove(item);
            return;
        }

        bool anyRemoved = false;
        foreach (var item in items)
        {
            if (_items.Remove(item)) anyRemoved = true;
        }

        if (anyRemoved)
        {
            OnPropertyChanged(nameof(Count));
            OnPropertyChanged(IndexerName);
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }
    }

    /// <summary>
    /// Removes the first occurrence of a specific object from the collection.
    /// </summary>
    /// <param name="item">The item to remove.</param>
    /// <returns>true if item was successfully removed; otherwise, false.</returns>
    public bool Remove(T item)
    {
        int index = _items.IndexOf(item);
        if (index < 0) return false;
        _items.RemoveAt(index);
        OnPropertyChanged(nameof(Count));
        OnPropertyChanged(IndexerName);
        CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item, index));
        return true;
    }

    /// <summary>
    /// Removes all items from the collection.
    /// </summary>
    public void Clear()
    {
        _items.Clear();
        OnPropertyChanged(nameof(Count));
        OnPropertyChanged(IndexerName);
        CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }

    /// <summary>
    /// Determines whether the collection contains a specific value.
    /// </summary>
    /// <param name="item">The item to locate in the collection.</param>
    /// <returns>true if item is found; otherwise, false.</returns>
    public bool Contains(T item) => _items.Contains(item);

    /// <summary>
    /// Copies the elements of the collection to an array, starting at a particular array index.
    /// </summary>
    /// <param name="array">The destination array.</param>
    /// <param name="arrayIndex">The zero-based index in array at which copying begins.</param>
    public void CopyTo(T[] array, int arrayIndex) => _items.CopyTo(array, arrayIndex);

    /// <summary>
    /// Returns an enumerator that iterates through the collection.
    /// </summary>
    /// <returns>An enumerator for the collection.</returns>
    public IEnumerator<T> GetEnumerator() => _items.GetEnumerator();

    /// <summary>
    /// Returns an enumerator that iterates through the collection (non-generic).
    /// </summary>
    /// <returns>An enumerator for the collection.</returns>
    IEnumerator IEnumerable.GetEnumerator() => _items.GetEnumerator();

    /// <summary>
    /// Determines the index of a specific item in the collection.
    /// </summary>
    /// <param name="item">The item to locate in the collection.</param>
    /// <returns>The index of item if found; otherwise, -1.</returns>
    public int IndexOf(T item) => _items.IndexOf(item);

    /// <summary>
    /// Inserts an item to the collection at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index at which item should be inserted.</param>
    /// <param name="item">The item to insert.</param>
    public void Insert(int index, T item)
    {
        _items.Insert(index, item);
        OnPropertyChanged(nameof(Count));
        OnPropertyChanged(IndexerName);
        CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, index));
    }

    /// <summary>
    /// Removes the item at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index of the item to remove.</param>
    public void RemoveAt(int index)
    {
        T oldItem = _items[index];
        _items.RemoveAt(index);
        OnPropertyChanged(nameof(Count));
        OnPropertyChanged(IndexerName);
        CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, oldItem, index));
    }

    void ICollection<T>.Add(T item) => Add(item);

    int IList.Add(object? value)
    {
        Add((T)value!);
        return _items.Count - 1;
    }

    bool IList.Contains(object? value) => value is T t && Contains(t);

    int IList.IndexOf(object? value) => value is T t ? IndexOf(t) : -1;

    void IList.Insert(int index, object? value) => Insert(index, (T)value!);

    void IList.Remove(object? value)
    {
        if (value is T t) Remove(t);
    }

    void ICollection.CopyTo(Array array, int index) => ((ICollection)_items).CopyTo(array, index);

    void IList.RemoveAt(int index) => RemoveAt(index);

    /// <summary>
    /// Moves the element at the specified old index to the new index and raises collection change notifications.
    /// </summary>
    /// <param name="oldIndex">The zero-based index of the item to move.</param>
    /// <param name="newIndex">The zero-based index to move the item to.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if oldIndex or newIndex is out of range.</exception>
    /// <remarks>
    /// Raises CollectionChanged (NotifyCollectionChangedAction.Move) and property change notification (Item[]).
    /// </remarks>
    public void Move(int oldIndex, int newIndex)
    {
        if (oldIndex < 0 || oldIndex >= _items.Count)
            throw new ArgumentOutOfRangeException(nameof(oldIndex));
        if (newIndex < 0 || newIndex >= _items.Count)
            throw new ArgumentOutOfRangeException(nameof(newIndex));
        if (oldIndex == newIndex) return;
        T item = _items[oldIndex];
        _items.RemoveAt(oldIndex);
        _items.Insert(newIndex, item);
        OnPropertyChanged(IndexerName);
        CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Move, item, newIndex, oldIndex));
    }

    /// <summary>
    /// Returns a read-only wrapper for the current list.
    /// </summary>
    /// <returns>A <see cref="ReadOnlyCollection{T}"/> that acts as a read-only wrapper around the current list.</returns>
    public ReadOnlyCollection<T> AsReadOnly()
    {
        return _items.AsReadOnly();
    }

    object? IList.this[int index]
    {
        get => this[index];
        set => this[index] = (T)value!;
    }

    bool IList.IsReadOnly => IsReadOnly;

    bool IList.IsFixedSize => IsFixedSize;

    object ICollection.SyncRoot => SyncRoot;

    bool ICollection.IsSynchronized => IsSynchronized;

    int IReadOnlyCollection<T>.Count => Count;

    T IReadOnlyList<T>.this[int index] => this[index];

    /// <summary>
    /// Raises the <see cref="PropertyChanged"/> event.
    /// </summary>
    /// <param name="propertyName">The name of the property that changed.</param>
    private void OnPropertyChanged(string propertyName)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

public interface IObservableCollection<T> : IList<T>, ICollection<T>, IEnumerable<T>, IEnumerable, IList, ICollection, IReadOnlyList<T>, IReadOnlyCollection<T>, INotifyCollectionChanged, INotifyPropertyChanged
{
    /// <summary>
    /// Adds the elements of the specified collection to the end of the list.
    /// </summary>
    /// <param name="items">The collection whose elements should be added.</param>
    public void AddRange(IEnumerable<T> items);

    /// <summary>
    /// Removes the first occurrence of each item from the specified collection.
    /// </summary>
    /// <param name="items">The collection whose elements should be removed.</param>
    public void RemoveRange(IEnumerable<T> items);

    /// <summary>
    /// Moves the element at the specified old index to the new index and raises collection change notifications.
    /// </summary>
    /// <param name="oldIndex">The zero-based index of the item to move.</param>
    /// <param name="newIndex">The zero-based index to move the item to.</param>
    public void Move(int oldIndex, int newIndex);
}
