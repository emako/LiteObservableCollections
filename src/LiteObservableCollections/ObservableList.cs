using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using LiteObservableCollections.Internals;

namespace LiteObservableCollections;

/// <summary>
/// A lite observable list that supports <see cref="INotifyCollectionChanged"/> and <see cref="INotifyPropertyChanged"/>.
/// </summary>
/// <summary>
/// Represents a list that notifies listeners of dynamic changes, such as when items get added, removed, or the whole list is refreshed.
/// </summary>
public partial class ObservableList<T> : IObservableList<T>, IDirectItemPropertyChangeSource<T>, INotifyCollectionChanged, INotifyPropertyChanged
{
    /// <summary>
    /// Indexer Name to notify that the this[] has changed.
    /// </summary>
    private const string IndexerName = "Item[]";

    private static readonly PropertyChangedEventArgs CountChangedEventArgs = new(nameof(Count));
    private static readonly PropertyChangedEventArgs IndexerChangedEventArgs = new(IndexerName);
    private static readonly NotifyCollectionChangedEventArgs ResetCollectionChangedEventArgs = new(NotifyCollectionChangedAction.Reset);

    /// <summary>
    /// The internal list storing the collection elements.
    /// </summary>
    private readonly List<T> _items;

    private ItemPropertyChangeHost<T>? _itemPropertyChangeHost;

    /// <summary>
    /// Gets or sets the optional event dispatcher. When set, <see cref="CollectionChanged"/>, <see cref="PropertyChanged"/>,
    /// and <see cref="ItemPropertyChanged"/> are raised on the dispatcher's context (e.g. UI thread).
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
    /// When false, modifications to the list do not raise outward events, but item <see cref="INotifyPropertyChanged"/>
    /// subscriptions used by <see cref="ItemPropertyChanged"/> remain synchronized with list contents. Default is true.
    /// </summary>
    public bool IsNotifyEnabled { get; set; } = true;

    /// <summary>
    /// Initializes a new empty ObservableList.
    /// </summary>
    public ObservableList()
    {
        _items = [];
    }

    /// <summary>
    /// Initializes a new ObservableList with the specified List.
    /// </summary>
    /// <param name="list">The List to initialize from.</param>
    /// <remarks>
    /// <paramref name="list"/> is wrapped, not copied. Mutating it directly bypasses change notifications and
    /// <see cref="ItemPropertyChanged"/> subscription tracking.
    /// </remarks>
    public ObservableList(List<T> list)
    {
        _items = list ?? [];
    }

    /// <summary>
    /// Initializes a new ObservableList with the specified collection.
    /// </summary>
    /// <param name="collection">The collection to initialize from.</param>
    public ObservableList(IEnumerable<T> collection)
    {
        _items = collection != null ? [.. collection] : [];
    }

    /// <summary>
    /// Initializes a new empty ObservableList and marshals change notifications to the specified synchronization context (e.g. UI thread).
    /// </summary>
    /// <param name="context">The context to raise CollectionChanged, PropertyChanged, and ItemPropertyChanged on; when null, notifications run on the current thread.</param>
    public ObservableList(SynchronizationContext? context) : this()
    {
        if (context != null) EventDispatcher = new SynchronizationContextCollectionEventDispatcher(context);
    }

    /// <summary>
    /// Initializes a new ObservableList with the specified list and marshals change notifications to the specified synchronization context (e.g. UI thread).
    /// </summary>
    /// <param name="context">The context to raise CollectionChanged, PropertyChanged, and ItemPropertyChanged on; when null, notifications run on the current thread.</param>
    /// <param name="list">The list to initialize from.</param>
    public ObservableList(SynchronizationContext? context, List<T> list) : this(list)
    {
        if (context != null) EventDispatcher = new SynchronizationContextCollectionEventDispatcher(context);
    }

    /// <summary>
    /// Initializes a new ObservableList with the specified collection and marshals change notifications to the specified synchronization context (e.g. UI thread).
    /// </summary>
    /// <param name="context">The context to raise CollectionChanged, PropertyChanged, and ItemPropertyChanged on; when null, notifications run on the current thread.</param>
    /// <param name="collection">The collection to initialize from.</param>
    public ObservableList(SynchronizationContext? context, IEnumerable<T> collection) : this(collection)
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
    /// Occurs when a property changes on an item currently contained in the list.
    /// Reference-type items that implement <see cref="INotifyPropertyChanged"/> are observed;
    /// other items are ignored.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This event is raised for item property changes only. For add, remove, replace, and reset notifications, use <see cref="CollectionChanged"/>.
    /// </para>
    /// <para>
    /// Items are subscribed when the first handler is attached and unsubscribed when the last one is removed, so the
    /// list does not listen to its items while nobody handles this event. While a handler is attached, every observed
    /// item references this list through its <see cref="INotifyPropertyChanged.PropertyChanged"/> event, so items that
    /// outlive the list keep it alive; remove handlers when they are no longer needed.
    /// </para>
    /// <para>
    /// Raising respects <see cref="IsNotifyEnabled"/> and is marshalled through <see cref="EventDispatcher"/> when set,
    /// matching <see cref="CollectionChanged"/> / <see cref="PropertyChanged"/>: both are checked when the item changes,
    /// and a marshalled event is delivered to the handlers attached at delivery time. A marshalled event is dropped if
    /// the item is removed before delivery, even if the same instance is added again in the meantime. Item subscriptions
    /// stay synchronized with the list contents even while notifications are disabled.
    /// </para>
    /// <para>
    /// Like the list itself, this event is not synchronized with mutations: handlers may be added and removed
    /// concurrently with each other, but not while another thread mutates the list. If subscribing to an item's
    /// <see cref="INotifyPropertyChanged.PropertyChanged"/> throws during a mutation, the mutation is still applied and
    /// its notifications are raised before the exception propagates.
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
        => _itemPropertyChangeHost ?? CreateItemPropertyChangeHost();

    private ItemPropertyChangeHost<T> CreateItemPropertyChangeHost()
    {
        ItemPropertyChangeHost<T> host = new(_items, this, () => IsNotifyEnabled, () => EventDispatcher);
        return Interlocked.CompareExchange(ref _itemPropertyChangeHost, host, null) ?? host;
    }

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
            try
            {
                _itemPropertyChangeHost?.OnReplaced(oldItem, value);
            }
            finally
            {
                RaisePropertyChanged(IndexerChangedEventArgs);
                RaiseCollectionReplaced(value, oldItem, index);
            }
        }
    }

    /// <summary>
    /// Gets the number of elements contained in the list.
    /// </summary>
    public int Count => _items.Count;

    /// <summary>
    /// Gets a value indicating whether the list is read-only.
    /// </summary>
    public bool IsReadOnly => false;

    /// <summary>
    /// Adds an item to the list.
    /// </summary>
    /// <param name="item">The object to add to the list.</param>
    public void Add(T item)
    {
        _items.Add(item);
        try
        {
            _itemPropertyChangeHost?.OnAdded(item);
        }
        finally
        {
            RaiseCountAndIndexerChanged();
            RaiseCollectionChanged(NotifyCollectionChangedAction.Add, item, _items.Count - 1);
        }
    }

    /// <summary>
    /// Adds the elements of the specified collection to the end of the list.
    /// </summary>
    /// <param name="items">The collection whose elements should be added to the end of the list.</param>
    public void AddRange(IEnumerable<T> items)
    {
        if (items == null) return;
        if (IsNotifyOnEachInRange)
        {
            foreach (T item in items)
                Add(item);
        }
        else
        {
            int startIndex = _items.Count;
            _items.AddRange(items);
            try
            {
                _itemPropertyChangeHost?.OnRangeAdded(startIndex);
            }
            finally
            {
                RaiseCountAndIndexerChanged();
                RaiseCollectionReset();
            }
        }
    }

    /// <summary>
    /// Removes the first occurrence of each item from the specified collection.
    /// </summary>
    /// <param name="items">The collection whose elements should be removed from the list.</param>
    public void RemoveRange(IEnumerable<T> items)
    {
        if (items == null) return;
        if (IsNotifyOnEachInRange)
        {
            foreach (T item in items)
                Remove(item);
            return;
        }

        bool anyRemoved = false;
        try
        {
            foreach (T item in items)
            {
                int index = _items.IndexOf(item);
                if (index < 0) continue;

                T removedItem = _items[index];
                _items.RemoveAt(index);
                anyRemoved = true;
                _itemPropertyChangeHost?.OnRemoved(removedItem);
            }
        }
        finally
        {
            if (anyRemoved)
            {
                RaiseCountAndIndexerChanged();
                RaiseCollectionReset();
            }
        }
    }

    /// <summary>
    /// Removes the first occurrence of a specific object from the list.
    /// </summary>
    /// <param name="item">The object to remove from the list.</param>
    /// <returns>true if item was successfully removed; otherwise, false.</returns>
    public bool Remove(T item)
    {
        int index = _items.IndexOf(item);
        if (index < 0) return false;

        T removedItem = _items[index];
        _items.RemoveAt(index);
        try
        {
            _itemPropertyChangeHost?.OnRemoved(removedItem);
        }
        finally
        {
            RaiseCountAndIndexerChanged();
            RaiseCollectionChanged(NotifyCollectionChangedAction.Remove, removedItem, index);
        }
        return true;
    }

    /// <summary>
    /// Removes all items from the list.
    /// </summary>
    public void Clear()
    {
        _items.Clear();
        try
        {
            _itemPropertyChangeHost?.OnCleared();
        }
        finally
        {
            RaiseCountAndIndexerChanged();
            RaiseCollectionReset();
        }
    }

    /// <summary>
    /// Resets the list to the specified items: clears the list, adds all items from the given enumeration,
    /// and raises a single <see cref="CollectionChanged"/> with <see cref="NotifyCollectionChangedAction.Reset"/>.
    /// </summary>
    /// <param name="items">The items to set. If null, the list is cleared.</param>
    public void Reset(IEnumerable<T>? items)
    {
        _items.Clear();
        if (items != null)
            _items.AddRange(items);
        try
        {
            _itemPropertyChangeHost?.OnReset();
        }
        finally
        {
            RaiseCountAndIndexerChanged();
            RaiseCollectionReset();
        }
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
    /// Determines whether the list contains a specific value.
    /// </summary>
    /// <param name="item">The object to locate in the list.</param>
    /// <returns>true if item is found; otherwise, false.</returns>
    public bool Contains(T item) => _items.Contains(item);

    /// <summary>
    /// Copies the elements of the list to an array, starting at a particular array index.
    /// </summary>
    /// <param name="array">The destination array.</param>
    /// <param name="arrayIndex">The zero-based index in array at which copying begins.</param>
    public void CopyTo(T[] array, int arrayIndex) => _items.CopyTo(array, arrayIndex);

    /// <summary>
    /// Returns an enumerator that iterates through the list.
    /// </summary>
    /// <returns>An enumerator for the list.</returns>
    public IEnumerator<T> GetEnumerator() => _items.GetEnumerator();

    /// <summary>
    /// Returns an enumerator that iterates through the list.
    /// </summary>
    /// <returns>An enumerator for the list.</returns>
    IEnumerator IEnumerable.GetEnumerator() => _items.GetEnumerator();

    /// <summary>
    /// Determines the index of a specific item in the list.
    /// </summary>
    /// <param name="item">The object to locate in the list.</param>
    /// <returns>The index of item if found; otherwise, -1.</returns>
    public int IndexOf(T item) => _items.IndexOf(item);

    /// <summary>
    /// Inserts an item to the list at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index at which item should be inserted.</param>
    /// <param name="item">The object to insert into the list.</param>
    public void Insert(int index, T item)
    {
        _items.Insert(index, item);
        try
        {
            _itemPropertyChangeHost?.OnAdded(item);
        }
        finally
        {
            RaiseCountAndIndexerChanged();
            RaiseCollectionChanged(NotifyCollectionChangedAction.Add, item, index);
        }
    }

    /// <summary>
    /// Removes the item at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index of the item to remove.</param>
    public void RemoveAt(int index)
    {
        T oldItem = _items[index];
        _items.RemoveAt(index);
        try
        {
            _itemPropertyChangeHost?.OnRemoved(oldItem);
        }
        finally
        {
            RaiseCountAndIndexerChanged();
            RaiseCollectionChanged(NotifyCollectionChangedAction.Remove, oldItem, index);
        }
    }

    /// <summary>
    /// Moves the element at the specified old index to the new index and raises collection change notifications.
    /// </summary>
    /// <param name="oldIndex">The zero-based index of the item to move.</param>
    /// <param name="newIndex">The zero-based index to move the item to.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if oldIndex or newIndex is out of range.</exception>
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
        RaisePropertyChanged(IndexerChangedEventArgs);
        RaiseCollectionMoved(item, newIndex, oldIndex);
    }

    /// <summary>
    /// Returns a read-only wrapper for the current list.
    /// </summary>
    /// <returns>A <see cref="ReadOnlyCollection{T}"/> that acts as a read-only wrapper around the current list.</returns>
    public ReadOnlyCollection<T> AsReadOnly()
    {
        return _items.AsReadOnly();
    }

    /// <summary>
    /// Reverses the order of the elements in the list.
    /// </summary>
    public void Reverse()
    {
        _items.Reverse();
        RaisePropertyChanged(IndexerChangedEventArgs);
        RaiseCollectionReset();
    }

    /// <summary>
    /// Sorts the elements in the list using the default comparer.
    /// </summary>
    public void Sort()
    {
        _items.Sort();
        RaisePropertyChanged(IndexerChangedEventArgs);
        RaiseCollectionReset();
    }

    /// <summary>
    /// Sorts the elements in the list using the specified comparer.
    /// </summary>
    /// <param name="comparer">The comparer to use when comparing elements.</param>
    public void Sort(IComparer<T> comparer)
    {
        _items.Sort(comparer);
        RaisePropertyChanged(IndexerChangedEventArgs);
        RaiseCollectionReset();
    }

    /// <summary>
    /// Sorts the elements in the list using the specified comparison.
    /// </summary>
    /// <param name="comparison">The comparison to use when comparing elements.</param>
    public void Sort(Comparison<T> comparison)
    {
        _items.Sort(comparison);
        RaisePropertyChanged(IndexerChangedEventArgs);
        RaiseCollectionReset();
    }

    /// <summary>
    /// Searches the entire sorted list for an element using the default comparer and returns the zero-based index of the element.
    /// </summary>
    /// <param name="item">The object to locate.</param>
    /// <returns>The zero-based index of item in the sorted list, if item is found; otherwise, a negative number that is the bitwise complement of the index of the next element that is larger than item or, if there is no larger element, the bitwise complement of <see cref="Count"/>.</returns>
    public int BinarySearch(T item)
        => _items.BinarySearch(item);

    /// <summary>
    /// Searches the entire sorted list for an element using the specified comparer and returns the zero-based index of the element.
    /// </summary>
    /// <param name="item">The object to locate.</param>
    /// <param name="comparer">The <see cref="IComparer{T}"/> implementation to use when comparing elements, or null to use the default comparer.</param>
    /// <returns>The zero-based index of item in the sorted list, if item is found; otherwise, a negative number that is the bitwise complement of the index of the next element that is larger than item or, if there is no larger element, the bitwise complement of <see cref="Count"/>.</returns>
    public int BinarySearch(T item, IComparer<T>? comparer)
        => _items.BinarySearch(item, comparer);

    /// <summary>
    /// Searches a range of elements in the sorted list for an element using the specified comparer and returns the zero-based index of the element.
    /// </summary>
    /// <param name="index">The zero-based starting index of the range to search.</param>
    /// <param name="count">The length of the range to search.</param>
    /// <param name="item">The object to locate.</param>
    /// <param name="comparer">The <see cref="IComparer{T}"/> implementation to use when comparing elements, or null to use the default comparer.</param>
    /// <returns>The zero-based index of item in the sorted list, if item is found; otherwise, a negative number that is the bitwise complement of the index of the next element that is larger than item or, if there is no larger element, the bitwise complement of (<paramref name="index"/> + <paramref name="count"/>).</returns>
    public int BinarySearch(int index, int count, T item, IComparer<T>? comparer)
        => _items.BinarySearch(index, count, item, comparer);

    private void RaiseCountAndIndexerChanged()
    {
        RaisePropertyChanged(CountChangedEventArgs);
        RaisePropertyChanged(IndexerChangedEventArgs);
    }

    private bool CanRaiseCollectionChanged => IsNotifyEnabled && CollectionChanged != null;

    private void RaiseCollectionChanged(NotifyCollectionChangedAction action, T item, int index)
    {
        if (CanRaiseCollectionChanged)
            RaiseCollectionChanged(new NotifyCollectionChangedEventArgs(action, item, index));
    }

    private void RaiseCollectionReplaced(T newItem, T oldItem, int index)
    {
        if (CanRaiseCollectionChanged)
            RaiseCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, newItem, oldItem, index));
    }

    private void RaiseCollectionMoved(T item, int newIndex, int oldIndex)
    {
        if (CanRaiseCollectionChanged)
            RaiseCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Move, item, newIndex, oldIndex));
    }

    private void RaiseCollectionReset() => RaiseCollectionChanged(ResetCollectionChangedEventArgs);

    /// <summary>
    /// Raises <see cref="CollectionChanged"/> on the dispatcher's context when <see cref="EventDispatcher"/> is set; otherwise raises on the current thread.
    /// </summary>
    private void RaiseCollectionChanged(NotifyCollectionChangedEventArgs e)
    {
        if (!CanRaiseCollectionChanged) return;
        ICollectionEventDispatcher? dispatcher = EventDispatcher;
        if (dispatcher != null && !dispatcher.IsCurrentContext)
        {
            PostCollectionChanged(dispatcher, e);
            return;
        }
        CollectionChanged?.Invoke(this, e);
    }

    /// <remarks>
    /// Kept separate from <see cref="RaiseCollectionChanged(NotifyCollectionChangedEventArgs)"/> so the closure is only allocated when posting.
    /// </remarks>
    private void PostCollectionChanged(ICollectionEventDispatcher dispatcher, NotifyCollectionChangedEventArgs e)
        => dispatcher.Post(() => CollectionChanged?.Invoke(this, e));

    /// <summary>
    /// Raises <see cref="PropertyChanged"/> on the dispatcher's context when <see cref="EventDispatcher"/> is set; otherwise raises on the current thread.
    /// </summary>
    private void RaisePropertyChanged(PropertyChangedEventArgs e)
    {
        if (!IsNotifyEnabled || PropertyChanged == null) return;
        ICollectionEventDispatcher? dispatcher = EventDispatcher;
        if (dispatcher != null && !dispatcher.IsCurrentContext)
        {
            PostPropertyChanged(dispatcher, e);
            return;
        }
        PropertyChanged?.Invoke(this, e);
    }

    /// <remarks>
    /// Kept separate from <see cref="RaisePropertyChanged"/> so the closure is only allocated when posting.
    /// </remarks>
    private void PostPropertyChanged(ICollectionEventDispatcher dispatcher, PropertyChangedEventArgs e)
        => dispatcher.Post(() => PropertyChanged?.Invoke(this, e));
}

/// <summary>
/// Defines an observable list interface that supports range addition.
/// </summary>
public interface IObservableList<T> : IList<T>, IItemPropertyObservable<T>, INotifyCollectionChanged, INotifyPropertyChanged
{
    /// <summary>
    /// Adds the elements of the specified collection to the end of the list.
    /// </summary>
    /// <param name="items">The collection whose elements should be added to the end of the list.</param>
    public void AddRange(IEnumerable<T> items);

    /// <summary>
    /// Resets the list to the specified items (clear then add all), raising a single CollectionChanged Reset.
    /// </summary>
    /// <param name="items">The items to set. If null, the list is cleared.</param>
    public void Reset(IEnumerable<T>? items);

    /// <summary>
    /// Removes the first occurrence of each item from the specified collection.
    /// </summary>
    /// <param name="items">The collection whose elements should be removed from the list.</param>
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
