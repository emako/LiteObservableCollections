using LiteObservableCollections.EventListeners;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace LiteObservableCollections.Internals;

/// <summary>
/// Maintains <see cref="INotifyPropertyChanged"/> subscriptions for items in a collection.
/// </summary>
internal sealed class CollectionItemPropertyChangeNotifier<T> : IDisposable
{
    private readonly IEnumerable<T> _items;
    private readonly object _eventOwner;
    private readonly Func<bool>? _canRaise;
    private readonly Func<ICollectionEventDispatcher?>? _getDispatcher;
    private readonly Dictionary<object, Subscription> _subscriptions = [with(ReferenceComparer.Instance)];
    private bool _disposed;

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

        foreach (T item in _items)
            Subscribe(item);
    }

    public event EventHandler<CollectionItemPropertyChangedEventArgs<T>>? ItemPropertyChanged;

    public bool HasHandlers => ItemPropertyChanged != null;

    public void HandleCollectionChanged(NotifyCollectionChangedEventArgs e)
    {
        if (_disposed) return;

        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                SubscribeItems(e.NewItems);
                break;

            case NotifyCollectionChangedAction.Remove:
                UnsubscribeItems(e.OldItems);
                break;

            case NotifyCollectionChangedAction.Replace:
                SubscribeItems(e.NewItems);
                UnsubscribeItems(e.OldItems);
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

        foreach (Subscription subscription in _subscriptions.Values)
            Detach(subscription);

        _subscriptions.Clear();
        ItemPropertyChanged = null;
    }

    private void OnItemPropertyChanged(Subscription subscription, PropertyChangedEventArgs e)
    {
        if (_disposed || !subscription.IsActive) return;
        if (_canRaise != null && !_canRaise()) return;

        EventHandler<CollectionItemPropertyChangedEventArgs<T>>? handlers = ItemPropertyChanged;
        if (handlers == null) return;

        CollectionItemPropertyChangedEventArgs<T> args = new(subscription.Item, e);
        ICollectionEventDispatcher? dispatcher = _getDispatcher?.Invoke();
        if (dispatcher != null && !dispatcher.IsCurrentContext)
        {
            dispatcher.Post(() =>
            {
                if (_disposed || !subscription.IsActive) return;
                if (_canRaise != null && !_canRaise()) return;
                handlers.Invoke(_eventOwner, args);
            });
            return;
        }

        handlers.Invoke(_eventOwner, args);
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

        subscription = new Subscription(item, observable);
        subscription.Handler = (_, e) => OnItemPropertyChanged(subscription, e);
        _subscriptions.Add(key, subscription);
        observable.PropertyChanged += subscription.Handler;
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

    private void ResetSubscriptions()
    {
        Dictionary<object, PendingSubscription> pending = [with(ReferenceComparer.Instance)];
        foreach (T item in _items)
        {
            if (!TryGetObservable(item, out object key, out _))
                continue;

            if (pending.TryGetValue(key, out PendingSubscription? existing))
            {
                existing.Count++;
            }
            else
            {
                pending.Add(key, new PendingSubscription(item));
            }
        }

        foreach (KeyValuePair<object, Subscription> pair in _subscriptions.ToArray())
        {
            if (pending.TryGetValue(pair.Key, out PendingSubscription? current))
            {
                pair.Value.Count = current.Count;
                pending.Remove(pair.Key);
                continue;
            }

            _subscriptions.Remove(pair.Key);
            Detach(pair.Value);
        }

        foreach (PendingSubscription current in pending.Values)
            Subscribe(current.Item);
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

    private sealed class Subscription(T item, INotifyPropertyChanged observable)
    {
        private int _isActive = 1;

        public T Item { get; } = item;

        public INotifyPropertyChanged Observable { get; } = observable;

        public PropertyChangedEventHandler Handler { get; set; } = null!;

        public int Count { get; set; } = 1;

        public bool IsActive => Volatile.Read(ref _isActive) != 0;

        public void Deactivate() => Volatile.Write(ref _isActive, 0);
    }

    private sealed class PendingSubscription(T item)
    {
        public T Item { get; } = item;

        public int Count { get; set; } = 1;
    }

    private sealed class ReferenceComparer : IEqualityComparer<object>
    {
        public static ReferenceComparer Instance { get; } = new();

        public new bool Equals(object? x, object? y) => ReferenceEquals(x, y);

        public int GetHashCode(object obj) => RuntimeHelpers.GetHashCode(obj);
    }
}
