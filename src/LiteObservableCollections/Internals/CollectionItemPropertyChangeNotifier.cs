using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using LiteObservableCollections.EventListeners;

namespace LiteObservableCollections.Internals;

/// <summary>
/// Maintains <see cref="INotifyPropertyChanged"/> subscriptions for items in a collection.
/// </summary>
internal sealed class CollectionItemPropertyChangeNotifier<T> : IDisposable where T : class, INotifyPropertyChanged
{
    private readonly IEnumerable<T> _items;
    private readonly object _eventOwner;
    private readonly Func<bool>? _canRaise;
    private readonly Func<ICollectionEventDispatcher?>? _getDispatcher;
    private readonly Dictionary<T, int> _subscriptionCounts = new(ReferenceComparer.Instance);
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
                UnsubscribeItems(e.OldItems);
                SubscribeItems(e.NewItems);
                break;

            case NotifyCollectionChangedAction.Reset:
                ResetSubscriptions();
                break;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;

        foreach (T item in _subscriptionCounts.Keys.ToArray())
            item.PropertyChanged -= OnItemPropertyChanged;

        _subscriptionCounts.Clear();
        ItemPropertyChanged = null;
        _disposed = true;
    }

    private void OnItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_disposed || sender is not T item) return;
        if (_canRaise != null && !_canRaise()) return;

        EventHandler<CollectionItemPropertyChangedEventArgs<T>>? handlers = ItemPropertyChanged;
        if (handlers == null) return;

        CollectionItemPropertyChangedEventArgs<T> args = new(item, e);
        ICollectionEventDispatcher? dispatcher = _getDispatcher?.Invoke();
        if (dispatcher != null && !dispatcher.IsCurrentContext)
        {
            dispatcher.Post(() =>
            {
                if (_disposed) return;
                ItemPropertyChanged?.Invoke(_eventOwner, args);
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
        if (item is null) return;

        if (_subscriptionCounts.TryGetValue(item, out int count))
        {
            _subscriptionCounts[item] = count + 1;
            return;
        }

        _subscriptionCounts.Add(item, 1);
        item.PropertyChanged += OnItemPropertyChanged;
    }

    private void Unsubscribe(T item)
    {
        if (item is null) return;
        if (!_subscriptionCounts.TryGetValue(item, out int count)) return;
        if (count > 1)
        {
            _subscriptionCounts[item] = count - 1;
            return;
        }

        _subscriptionCounts.Remove(item);
        item.PropertyChanged -= OnItemPropertyChanged;
    }

    private void ResetSubscriptions()
    {
        foreach (T item in _subscriptionCounts.Keys.ToArray())
            item.PropertyChanged -= OnItemPropertyChanged;

        _subscriptionCounts.Clear();

        foreach (T item in _items)
            Subscribe(item);
    }

    private sealed class ReferenceComparer : IEqualityComparer<T>
    {
        public static ReferenceComparer Instance { get; } = new();

        public bool Equals(T? x, T? y) => ReferenceEquals(x, y);

        public int GetHashCode(T obj) => RuntimeHelpers.GetHashCode(obj);
    }
}
