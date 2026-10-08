using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace LiteObservableCollections.Internals;

/// <summary>
/// Maintains <see cref="INotifyPropertyChanged"/> subscriptions for items in a collection.
/// </summary>
/// <remarks>
/// Not thread-safe: the owner must serialize construction, the mutation methods, <see cref="HandleCollectionChanged"/>,
/// and <see cref="Dispose"/>. Item property changes may be raised on any thread concurrently with those calls.
/// </remarks>
internal sealed class CollectionItemPropertyChangeNotifier<T> : IDisposable
{
    private static readonly bool CanObserveItems = !typeof(T).IsValueType;

    private readonly IEnumerable<T> _items;
    private readonly object _eventOwner;
    private readonly Func<bool>? _canRaise;
    private readonly Func<ICollectionEventDispatcher?>? _getDispatcher;
    private readonly Dictionary<object, Subscription> _subscriptions = [with(ReferenceComparer.Instance)];
    private volatile bool _disposed;

    public CollectionItemPropertyChangeNotifier(
        IEnumerable<T> items,
        object eventOwner,
        Func<bool>? canRaise = null,
        Func<ICollectionEventDispatcher?>? getDispatcher = null)
    {
        _items = items ?? throw new ArgumentNullException(nameof(items));
        _eventOwner = eventOwner ?? throw new ArgumentNullException(nameof(eventOwner));
        _canRaise = canRaise;
        _getDispatcher = getDispatcher;

        if (!CanObserveItems) return;

        try
        {
            foreach (T item in _items)
                Subscribe(item);
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    /// <summary>
    /// Raised subject to <c>canRaise</c> and marshalled through the dispatcher, like the owner's other notifications.
    /// </summary>
    public event EventHandler<ItemPropertyChangedEventArgs<T>>? ItemPropertyChanged;

    /// <summary>
    /// Raised synchronously on the thread that raised the item's <see cref="INotifyPropertyChanged.PropertyChanged"/>,
    /// ignoring <c>canRaise</c> and the dispatcher.
    /// </summary>
    public event EventHandler<ItemPropertyChangedEventArgs<T>>? DirectItemPropertyChanged;

    public bool HasHandlers => ItemPropertyChanged != null || DirectItemPropertyChanged != null;

    public void Add(T item)
    {
        if (_disposed || !CanObserveItems) return;
        Subscribe(item);
    }

    public void AddRange(IReadOnlyList<T> items, int startIndex)
    {
        if (_disposed || !CanObserveItems) return;
        for (int i = startIndex; i < items.Count; i++)
            Subscribe(items[i]);
    }

    public void Remove(T item)
    {
        if (_disposed || !CanObserveItems) return;
        Unsubscribe(item);
    }

    public void Replace(T oldItem, T newItem)
    {
        if (_disposed || !CanObserveItems) return;
        try
        {
            Subscribe(newItem);
        }
        finally
        {
            Unsubscribe(oldItem);
        }
    }

    public void Clear()
    {
        if (_disposed) return;
        DetachAll();
    }

    /// <summary>
    /// Re-synchronizes subscriptions with the current source contents without re-attaching items that are still present.
    /// </summary>
    public void Reset()
    {
        if (_disposed || !CanObserveItems) return;
        ResetSubscriptions();
    }

    public void HandleCollectionChanged(NotifyCollectionChangedEventArgs e)
    {
        if (_disposed || !CanObserveItems) return;

        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                SubscribeItems(e.NewItems);
                break;

            case NotifyCollectionChangedAction.Remove:
                UnsubscribeItems(e.OldItems);
                break;

            case NotifyCollectionChangedAction.Replace:
                try
                {
                    SubscribeItems(e.NewItems);
                }
                finally
                {
                    UnsubscribeItems(e.OldItems);
                }
                break;

            case NotifyCollectionChangedAction.Reset:
                ResetSubscriptions();
                break;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        DetachAll();

        ItemPropertyChanged = null;
        DirectItemPropertyChanged = null;
    }

    private void OnItemPropertyChanged(Subscription subscription, PropertyChangedEventArgs e)
    {
        if (_disposed || !subscription.IsActive) return;

        EventHandler<ItemPropertyChangedEventArgs<T>>? direct = DirectItemPropertyChanged;
        bool raiseNotified = ItemPropertyChanged != null && (_canRaise == null || _canRaise());
        if (direct == null && !raiseNotified) return;

        ItemPropertyChangedEventArgs<T> args = new(subscription.Item, e);
        try
        {
            direct?.Invoke(_eventOwner, args);
        }
        finally
        {
            if (raiseNotified)
                RaiseItemPropertyChanged(subscription, args);
        }
    }

    private void RaiseItemPropertyChanged(Subscription subscription, ItemPropertyChangedEventArgs<T> args)
    {
        ICollectionEventDispatcher? dispatcher = _getDispatcher?.Invoke();
        if (dispatcher != null && !dispatcher.IsCurrentContext)
        {
            PostItemPropertyChanged(dispatcher, subscription, args);
            return;
        }

        ItemPropertyChanged?.Invoke(_eventOwner, args);
    }

    /// <remarks>
    /// Kept separate from <see cref="RaiseItemPropertyChanged"/> so the closure is only allocated when posting.
    /// </remarks>
    private void PostItemPropertyChanged(
        ICollectionEventDispatcher dispatcher,
        Subscription subscription,
        ItemPropertyChangedEventArgs<T> args)
    {
        dispatcher.Post(() =>
        {
            if (_disposed || !subscription.IsActive) return;
            ItemPropertyChanged?.Invoke(_eventOwner, args);
        });
    }

    private void SubscribeItems(System.Collections.IList? items)
    {
        if (items == null) return;
        foreach (object? item in items)
        {
            if (item is T typedItem)
                Subscribe(typedItem);
        }
    }

    private void UnsubscribeItems(System.Collections.IList? items)
    {
        if (items == null) return;
        foreach (object? item in items)
        {
            if (item is T typedItem)
                Unsubscribe(typedItem);
        }
    }

    private void Subscribe(T item)
    {
        if (!TryGetObservable(item, out object key, out INotifyPropertyChanged observable))
            return;

        if (_subscriptions.TryGetValue(key, out Subscription? subscription))
        {
            subscription.Count++;
            return;
        }

        Attach(key, item, observable, count: 1);
    }

    private void Unsubscribe(T item)
    {
        if (!TryGetObservable(item, out object key, out _))
            return;
        if (!_subscriptions.TryGetValue(key, out Subscription? subscription))
            return;
        if (subscription.Count > 1)
        {
            subscription.Count--;
            return;
        }

        _subscriptions.Remove(key);
        Detach(subscription);
    }

    private void Attach(object key, T item, INotifyPropertyChanged observable, int count)
    {
        Subscription subscription = new(this, item, observable) { Count = count };
        observable.PropertyChanged += subscription.Handler;
        _subscriptions.Add(key, subscription);
    }

    private void DetachAll()
    {
        foreach (Subscription subscription in _subscriptions.Values)
            Detach(subscription);

        _subscriptions.Clear();
    }

    private void ResetSubscriptions()
    {
        Dictionary<object, int> pending = new(_items is ICollection<T> collection ? collection.Count : 0, ReferenceComparer.Instance);
        foreach (T item in _items)
        {
            if (!TryGetObservable(item, out object key, out _))
                continue;

            pending.TryGetValue(key, out int count);
            pending[key] = count + 1;
        }

        foreach (KeyValuePair<object, Subscription> pair in _subscriptions.ToArray())
        {
            if (pending.TryGetValue(pair.Key, out int count))
            {
                pair.Value.Count = count;
                pending.Remove(pair.Key);
                continue;
            }

            _subscriptions.Remove(pair.Key);
            Detach(pair.Value);
        }

        foreach (KeyValuePair<object, int> pair in pending)
            Attach(pair.Key, (T)pair.Key, (INotifyPropertyChanged)pair.Key, pair.Value);
    }

    private static bool TryGetObservable(
        T item,
        out object key,
        out INotifyPropertyChanged observable)
    {
        object? candidate = item;
        if (candidate is INotifyPropertyChanged propertyChanged && !candidate.GetType().IsValueType)
        {
            key = candidate;
            observable = propertyChanged;
            return true;
        }

        key = null!;
        observable = null!;
        return false;
    }

    private static void Detach(Subscription subscription)
    {
        subscription.Deactivate();
        subscription.Observable.PropertyChanged -= subscription.Handler;
    }

    private sealed class Subscription
    {
        private readonly CollectionItemPropertyChangeNotifier<T> _owner;
        private int _isActive = 1;

        public Subscription(CollectionItemPropertyChangeNotifier<T> owner, T item, INotifyPropertyChanged observable)
        {
            _owner = owner;
            Item = item;
            Observable = observable;
            Handler = OnPropertyChanged;
        }

        public T Item { get; }

        public INotifyPropertyChanged Observable { get; }

        public PropertyChangedEventHandler Handler { get; }

        public int Count { get; set; }

        public bool IsActive => Volatile.Read(ref _isActive) != 0;

        public void Deactivate() => Volatile.Write(ref _isActive, 0);

        private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e) => _owner.OnItemPropertyChanged(this, e);
    }

    private sealed class ReferenceComparer : IEqualityComparer<object>
    {
        public static ReferenceComparer Instance { get; } = new();

        public new bool Equals(object? x, object? y) => ReferenceEquals(x, y);

        public int GetHashCode(object obj) => RuntimeHelpers.GetHashCode(obj);
    }
}
