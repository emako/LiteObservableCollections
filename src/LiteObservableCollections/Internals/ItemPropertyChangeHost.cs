using LiteObservableCollections.EventListeners;

namespace LiteObservableCollections.Internals;

/// <summary>
/// Hosts item-level property change notifications for a list-backed observable collection.
/// Item subscriptions exist only while at least one handler is attached.
/// </summary>
/// <remarks>
/// Adding and removing handlers is thread-safe. The mutation hooks must be called by the owner right after it
/// mutates <c>items</c>, so subscriptions stay synchronized even when outward notifications are suppressed.
/// </remarks>
internal sealed class ItemPropertyChangeHost<T>(
    List<T> items,
    object eventOwner,
    Func<bool> canRaise,
    Func<ICollectionEventDispatcher?> getDispatcher)
{
    private readonly object _gate = new();
    private volatile CollectionItemPropertyChangeNotifier<T>? _notifier;

    public void AddHandler(EventHandler<CollectionItemPropertyChangedEventArgs<T>> handler)
    {
        lock (_gate)
            EnsureNotifier().ItemPropertyChanged += handler;
    }

    public void RemoveHandler(EventHandler<CollectionItemPropertyChangedEventArgs<T>>? handler)
    {
        lock (_gate)
        {
            if (_notifier == null) return;
            _notifier.ItemPropertyChanged -= handler;
            ReleaseNotifierIfUnused();
        }
    }

    public void AddDirectHandler(EventHandler<CollectionItemPropertyChangedEventArgs<T>> handler)
    {
        lock (_gate)
            EnsureNotifier().DirectItemPropertyChanged += handler;
    }

    public void RemoveDirectHandler(EventHandler<CollectionItemPropertyChangedEventArgs<T>> handler)
    {
        lock (_gate)
        {
            if (_notifier == null) return;
            _notifier.DirectItemPropertyChanged -= handler;
            ReleaseNotifierIfUnused();
        }
    }

    public void OnAdded(T item) => _notifier?.Add(item);

    public void OnRangeAdded(int startIndex) => _notifier?.AddRange(items, startIndex);

    public void OnRemoved(T item) => _notifier?.Remove(item);

    public void OnReplaced(T oldItem, T newItem) => _notifier?.Replace(oldItem, newItem);

    public void OnCleared() => _notifier?.Clear();

    public void OnReset() => _notifier?.Reset();

    private CollectionItemPropertyChangeNotifier<T> EnsureNotifier()
        => _notifier ??= new CollectionItemPropertyChangeNotifier<T>(items, eventOwner, canRaise, getDispatcher);

    private void ReleaseNotifierIfUnused()
    {
        if (_notifier == null || _notifier.HasHandlers) return;

        _notifier.Dispose();
        _notifier = null;
    }
}
