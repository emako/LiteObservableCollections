using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using LiteObservableCollections.EventListeners;

namespace LiteObservableCollections.Internals;

/// <summary>
/// Hosts item-level property change notifications for observable collections.
/// </summary>
internal sealed class ObservableCollectionItemPropertyChangeHost<T>
{
    private readonly IEnumerable<T> _items;
    private readonly object _eventOwner;
    private readonly Func<bool> _canRaise;
    private readonly Func<ICollectionEventDispatcher?> _getDispatcher;
    private CollectionItemPropertyChangeNotifier<T>? _notifier;

    [SuppressMessage("Style", "IDE0290:Use primary constructor")]
    public ObservableCollectionItemPropertyChangeHost(
        IEnumerable<T> items,
        object eventOwner,
        Func<bool> canRaise,
        Func<ICollectionEventDispatcher?> getDispatcher)
    {
        _items = items ?? throw new ArgumentNullException(nameof(items));
        _eventOwner = eventOwner ?? throw new ArgumentNullException(nameof(eventOwner));
        _canRaise = canRaise ?? throw new ArgumentNullException(nameof(canRaise));
        _getDispatcher = getDispatcher ?? throw new ArgumentNullException(nameof(getDispatcher));
    }

    public void AddHandler(EventHandler<CollectionItemPropertyChangedEventArgs<T>> handler)
        => EnsureNotifier().ItemPropertyChanged += handler;

    public void RemoveHandler(EventHandler<CollectionItemPropertyChangedEventArgs<T>>? handler)
    {
        if (_notifier == null) return;

        _notifier.ItemPropertyChanged -= handler;
        if (_notifier.HasHandlers) return;

        _notifier.Dispose();
        _notifier = null;
    }

    public void HandleCollectionChanged(NotifyCollectionChangedEventArgs e)
        => _notifier?.HandleCollectionChanged(e);

    private CollectionItemPropertyChangeNotifier<T> EnsureNotifier()
    {
        return _notifier ??= new CollectionItemPropertyChangeNotifier<T>(
            _items,
            _eventOwner,
            _canRaise,
            _getDispatcher);
    }
}
