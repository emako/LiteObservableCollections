using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using LiteObservableCollections.Internals;

namespace LiteObservableCollections;

/// <summary>
/// A lite observable collection that supports <see cref="INotifyCollectionChanged"/> and <see cref="INotifyPropertyChanged"/>, and supports AddRange for batch addition.
/// </summary>
/// <typeparam name="T">The type of elements in the collection.</typeparam>
public class ObservableCollection<T> : IObservableCollection<T>, IDirectItemPropertyChangeSource<T>, INotifyCollectionChanged, INotifyPropertyChanged
{
    /// <summary>
    /// Indexer Name to notify that the this[] has changed.
    /// </summary>
    private const string IndexerName = "Item[]";

    /// <summary>
    /// The internal list storing the collection elements.
    /// </summary>
    private readonly List<T> _items;

    private ItemPropertyChangeHost<T>? _itemPropertyChangeHost;

    /// <summary>
    /// Gets or sets the optional event dispatcher. When set, CollectionChanged and PropertyChanged are raised on the dispatcher's context (e.g. UI thread).
    /// </summary>
    public ICollectionEventDispatcher? EventDispatcher { get; set; }

    /// <summary>
    /// Indicates the AddRange or RemoveRange notification behavior.
    /// If true, AddRange or RemoveRange will trigger CollectionChanged with `NotifyCollectionChangedAction.Add` or `NotifyCollectionChangedAction.Remove` for each item added or removed;
    /// otherwise, it will only trigger once at the end with `NotifyCollectionChangedAction.Reset`.
    /// This allows for more granular change notifications, enabling UI scenarios such as per-item insertion or deletion animations.
    /// </summary>
    public bool IsNotifyOnEachInRange { get; set; } = false;

    /// <summary>
    /// Gets or sets whether change notifications (<see cref="CollectionChanged"/>, <see cref="PropertyChanged"/>, and
    /// <see cref="ItemPropertyChanged"/>) are raised.
    /// When false, modifications to the collection do not raise outward events, but item <see cref="INotifyPropertyChanged"/>
    /// subscriptions used by <see cref="ItemPropertyChanged"/> remain synchronized with collection contents. Default is true.
    /// </summary>
    public bool IsNotifyEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets whether item <see cref="INotifyPropertyChanged"/> subscriptions are maintained so that
    /// <see cref="ItemPropertyChanged"/> can be raised.
    /// The default is false. While false, the collection does not listen to item property changes, and
    /// <see cref="ItemPropertyChanged"/> is not raised even if handlers are attached.
    /// Set this property to true to enable the feature and receive the event.
    /// </summary>
    public bool IsItemPropertyChangedEnabled
    {
        get => field;
        set
        {
            if (field == value)
                return;

            field = value;
            _itemPropertyChangeHost?.OnItemPropertyChangedEnabledChanged();
        }
    }

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
    /// Initializes a new empty ObservableCollection and marshals change notifications to the specified synchronization context (e.g. UI thread).
    /// </summary>
    /// <param name="context">The context to raise CollectionChanged and PropertyChanged on; when null, notifications run on the current thread.</param>
    public ObservableCollection(SynchronizationContext? context) : this()
    {
        if (context != null) EventDispatcher = new SynchronizationContextCollectionEventDispatcher(context);
    }

    /// <summary>
    /// Initializes a new ObservableCollection with the specified collection and marshals change notifications to the specified synchronization context (e.g. UI thread).
    /// </summary>
    /// <param name="context">The context to raise CollectionChanged and PropertyChanged on; when null, notifications run on the current thread.</param>
    /// <param name="collection">The collection to initialize from.</param>
    public ObservableCollection(SynchronizationContext? context, IEnumerable<T> collection) : this(collection)
    {
        if (context != null) EventDispatcher = new SynchronizationContextCollectionEventDispatcher(context);
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
    /// Occurs when a property changes on an item currently contained in the collection.
    /// Reference-type items that implement <see cref="INotifyPropertyChanged"/> are observed;
    /// other items are ignored.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This event is raised for item property changes only. For add, remove, replace, and reset notifications, use <see cref="CollectionChanged"/>.
    /// </para>
    /// <para>
    /// Raising respects <see cref="IsNotifyEnabled"/> and is marshalled through <see cref="EventDispatcher"/> when set,
    /// matching <see cref="CollectionChanged"/> / <see cref="PropertyChanged"/>: both are checked when the item changes,
    /// and a marshalled event is delivered to the handlers attached at delivery time. A marshalled event is dropped if
    /// the item has been removed before delivery. Item subscriptions stay synchronized with the collection contents even
    /// while notifications are disabled.
    /// </para>
    /// <para>
    /// Handlers do not receive this event unless <see cref="IsItemPropertyChangedEnabled"/> is true.
    /// The default is false: item <see cref="INotifyPropertyChanged"/> subscriptions are not created, which avoids
    /// the cost of listening to every item. Set <see cref="IsItemPropertyChangedEnabled"/> to true to enable
    /// listening and receive the event. While enabled, items are subscribed when the first handler is attached and
    /// unsubscribed when the last one is removed or the property is set back to false.
    /// While a handler is attached and the feature is enabled, every observed item references this collection through its
    /// <see cref="INotifyPropertyChanged.PropertyChanged"/> event, so items that outlive the collection keep it alive;
    /// remove handlers when they are no longer needed. Adding and removing handlers is thread-safe.
    /// </para>
    /// </remarks>
    public event EventHandler<ItemPropertyChangedEventArgs<T>>? ItemPropertyChanged
    {
        add
        {
            if (value != null)
                ItemPropertyChangeHost.AddHandler(value);
        }
        remove => _itemPropertyChangeHost?.RemoveHandler(value);
    }

    void IDirectItemPropertyChangeSource<T>.AddDirectItemPropertyChangedHandler(EventHandler<ItemPropertyChangedEventArgs<T>> handler)
        => ItemPropertyChangeHost.AddDirectHandler(handler);

    void IDirectItemPropertyChangeSource<T>.RemoveDirectItemPropertyChangedHandler(EventHandler<ItemPropertyChangedEventArgs<T>> handler)
        => _itemPropertyChangeHost?.RemoveDirectHandler(handler);

    private ItemPropertyChangeHost<T> ItemPropertyChangeHost
        => LazyInitializer.EnsureInitialized(ref _itemPropertyChangeHost, () => new ItemPropertyChangeHost<T>(
            _items,
            this,
            () => IsNotifyEnabled,
            () => EventDispatcher,
            () => IsItemPropertyChangedEnabled))!;

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
            _itemPropertyChangeHost?.OnReplaced(oldItem, value);
            OnPropertyChanged(IndexerName);
            RaiseCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, value, oldItem, index));
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
        _itemPropertyChangeHost?.OnAdded(item);
        OnPropertyChanged(nameof(Count));
        OnPropertyChanged(IndexerName);
        RaiseCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, _items.Count - 1));
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
            int startIndex = _items.Count;
            _items.AddRange(items);
            _itemPropertyChangeHost?.OnRangeAdded(startIndex);
            OnPropertyChanged(nameof(Count));
            OnPropertyChanged(IndexerName);
            RaiseCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
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
        foreach (T item in items)
        {
            int index = _items.IndexOf(item);
            if (index < 0) continue;

            T removedItem = _items[index];
            _items.RemoveAt(index);
            _itemPropertyChangeHost?.OnRemoved(removedItem);
            anyRemoved = true;
        }

        if (anyRemoved)
        {
            OnPropertyChanged(nameof(Count));
            OnPropertyChanged(IndexerName);
            RaiseCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
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
        T removedItem = _items[index];
        _items.RemoveAt(index);
        _itemPropertyChangeHost?.OnRemoved(removedItem);
        OnPropertyChanged(nameof(Count));
        OnPropertyChanged(IndexerName);
        RaiseCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, removedItem, index));
        return true;
    }

    /// <summary>
    /// Removes all items from the collection.
    /// </summary>
    public void Clear()
    {
        _items.Clear();
        _itemPropertyChangeHost?.OnCleared();
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
        _items.Clear();
        if (items != null)
            _items.AddRange(items);
        _itemPropertyChangeHost?.OnReset();
        OnPropertyChanged(nameof(Count));
        OnPropertyChanged(IndexerName);
        RaiseCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }

    /// <summary>
    /// Synchronizes this list with the specified collection while preserving existing items at matching indexes.
    /// </summary>
    /// <param name="items">The collection to synchronize with.</param>
    /// <param name="assign">Copies data from a source item to the existing item at the same index. The index is zero-based.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="items"/> or <paramref name="assign"/> is null.</exception>
    public void Sync(IEnumerable<T> items, Action<int, T, T> assign)
    {
        if (items == null) throw new ArgumentNullException(nameof(items));
        if (assign == null) throw new ArgumentNullException(nameof(assign));

        int index = 0;
        foreach (T item in items)
        {
            if (index < _items.Count)
                assign(index, _items[index], item);
            else
                Add(item);

            index++;
        }

        for (int removeIndex = _items.Count - 1; removeIndex >= index; removeIndex--)
            RemoveAt(removeIndex);
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
        _itemPropertyChangeHost?.OnAdded(item);
        OnPropertyChanged(nameof(Count));
        OnPropertyChanged(IndexerName);
        RaiseCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, index));
    }

    /// <summary>
    /// Removes the item at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index of the item to remove.</param>
    public void RemoveAt(int index)
    {
        T oldItem = _items[index];
        _items.RemoveAt(index);
        _itemPropertyChangeHost?.OnRemoved(oldItem);
        OnPropertyChanged(nameof(Count));
        OnPropertyChanged(IndexerName);
        RaiseCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, oldItem, index));
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
        RaiseCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Move, item, newIndex, oldIndex));
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
        => RaisePropertyChanged(new PropertyChangedEventArgs(propertyName));

    /// <summary>
    /// Raises <see cref="CollectionChanged"/> on the dispatcher's context when <see cref="EventDispatcher"/> is set; otherwise raises on the current thread.
    /// </summary>
    private void RaiseCollectionChanged(NotifyCollectionChangedEventArgs e)
    {
        if (!IsNotifyEnabled || CollectionChanged == null) return;
        if (EventDispatcher != null && !EventDispatcher.IsCurrentContext)
        {
            EventDispatcher.Post(() => CollectionChanged?.Invoke(this, e));
            return;
        }
        CollectionChanged.Invoke(this, e);
    }

    /// <summary>
    /// Raises <see cref="PropertyChanged"/> on the dispatcher's context when <see cref="EventDispatcher"/> is set; otherwise raises on the current thread.
    /// </summary>
    private void RaisePropertyChanged(PropertyChangedEventArgs e)
    {
        if (!IsNotifyEnabled || PropertyChanged == null) return;
        if (EventDispatcher != null && !EventDispatcher.IsCurrentContext)
        {
            EventDispatcher.Post(() => PropertyChanged?.Invoke(this, e));
            return;
        }
        PropertyChanged.Invoke(this, e);
    }
}

public interface IObservableCollection<T> : IList<T>, ICollection<T>, IEnumerable<T>, IEnumerable, IList, ICollection, IReadOnlyList<T>, IReadOnlyCollection<T>, IItemPropertyObservable<T>, INotifyCollectionChanged, INotifyPropertyChanged
{
    /// <summary>
    /// Adds the elements of the specified collection to the end of the list.
    /// </summary>
    /// <param name="items">The collection whose elements should be added.</param>
    public void AddRange(IEnumerable<T> items);

    /// <summary>
    /// Resets the collection to the specified items (clear then add all), raising a single CollectionChanged Reset.
    /// </summary>
    /// <param name="items">The items to set. If null, the collection is cleared.</param>
    public void Reset(IEnumerable<T>? items);

    /// <summary>
    /// Removes the first occurrence of each item from the specified collection.
    /// </summary>
    /// <param name="items">The collection whose elements should be removed.</param>
    public void RemoveRange(IEnumerable<T> items);

    /// <summary>
    /// Synchronizes this list with the specified collection while preserving existing items at matching indexes.
    /// </summary>
    /// <param name="items">The collection to synchronize with.</param>
    /// <param name="assign">Copies data from a source item to the existing item at the same index. The index is zero-based.</param>
    public void Sync(IEnumerable<T> items, Action<int, T, T> assign);

    /// <summary>
    /// Moves the element at the specified old index to the new index and raises collection change notifications.
    /// </summary>
    /// <param name="oldIndex">The zero-based index of the item to move.</param>
    /// <param name="newIndex">The zero-based index to move the item to.</param>
    public void Move(int oldIndex, int newIndex);
}
