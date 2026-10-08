namespace LiteObservableCollections.Internals;

/// <summary>
/// Hosts item-level property change notifications for a list-backed observable collection.
/// Public <c>ItemPropertyChanged</c> subscriptions exist only while that feature is enabled and at least one
/// handler is attached. Direct listeners opt in independently.
/// </summary>
/// <remarks>
/// Adding and removing handlers is thread-safe. The mutation hooks must be called by the owner right after it
/// mutates <c>items</c>, so subscriptions stay synchronized even when outward notifications are suppressed.
/// </remarks>
internal sealed class ItemPropertyChangeHost<T>(
    List<T> items,
    object eventOwner,
    Func<bool> canRaise,
    Func<ICollectionEventDispatcher?> getDispatcher,
    Func<bool> isItemPropertyChangedEnabled)
{
    private readonly object _gate = new();
    private volatile CollectionItemPropertyChangeNotifier<T>? _notifier;
    private EventHandler<ItemPropertyChangedEventArgs<T>>? _handlers;
    private bool _listening;

    public void AddHandler(EventHandler<ItemPropertyChangedEventArgs<T>> handler)
    {
        lock (_gate)
        {
            _handlers += handler;
            try
            {
                if (isItemPropertyChangedEnabled())
                    AttachPublicListener();
            }
            catch
            {
                _handlers -= handler;
                throw;
            }
        }
    }

    public void RemoveHandler(EventHandler<ItemPropertyChangedEventArgs<T>>? handler)
    {
        lock (_gate)
        {
            _handlers -= handler;
            if (_handlers == null)
                DetachPublicListener();
        }
    }

    public void OnItemPropertyChangedEnabledChanged()
    {
        lock (_gate)
        {
            if (isItemPropertyChangedEnabled())
                AttachPublicListener();
            else
                DetachPublicListener();
        }
    }

    public void AddDirectHandler(EventHandler<ItemPropertyChangedEventArgs<T>> handler)
    {
        lock (_gate)
            EnsureNotifier().DirectItemPropertyChanged += handler;
    }

    public void RemoveDirectHandler(EventHandler<ItemPropertyChangedEventArgs<T>> handler)
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

    private void AttachPublicListener()
    {
        if (_listening || _handlers == null) return;

        EnsureNotifier().ItemPropertyChanged += ForwardItemPropertyChanged;
        _listening = true;
    }

    private void DetachPublicListener()
    {
        if (!_listening) return;

        _notifier!.ItemPropertyChanged -= ForwardItemPropertyChanged;
        _listening = false;
        ReleaseNotifierIfUnused();
    }

    private void ForwardItemPropertyChanged(object? sender, ItemPropertyChangedEventArgs<T> e)
        => _handlers?.Invoke(sender, e);

    private CollectionItemPropertyChangeNotifier<T> EnsureNotifier()
        => _notifier ??= new CollectionItemPropertyChangeNotifier<T>(items, eventOwner, canRaise, getDispatcher);

    private void ReleaseNotifierIfUnused()
    {
        if (_notifier == null || _notifier.HasHandlers) return;

        _notifier.Dispose();
        _notifier = null;
    }
}
