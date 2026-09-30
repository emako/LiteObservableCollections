using System.Collections.Specialized;
using System.ComponentModel;
using LiteObservableCollections.EventListeners;

namespace LiteObservableCollections.Internals;

internal interface IItemPropertyChangeSink : IDisposable
{
    void HandleCollectionChanged(NotifyCollectionChangedEventArgs e);

    void AddHandler(Delegate handler);

    void RemoveHandler(Delegate? handler);

    bool HasHandlers { get; }
}

internal sealed class ItemPropertyChangeSink<T> : IItemPropertyChangeSink where T : class, INotifyPropertyChanged
{
    private readonly CollectionItemPropertyChangeNotifier<T> _notifier;

    public ItemPropertyChangeSink(
        IEnumerable<T> items,
        object eventOwner,
        Func<bool>? canRaise,
        Func<ICollectionEventDispatcher?>? getDispatcher)
        => _notifier = new CollectionItemPropertyChangeNotifier<T>(items, eventOwner, canRaise, getDispatcher);

    public bool HasHandlers => _notifier.HasHandlers;

    public void HandleCollectionChanged(NotifyCollectionChangedEventArgs e)
        => _notifier.HandleCollectionChanged(e);

    public void AddHandler(Delegate handler)
        => _notifier.ItemPropertyChanged += (EventHandler<CollectionItemPropertyChangedEventArgs<T>>)handler;

    public void RemoveHandler(Delegate? handler)
    {
        if (handler != null)
            _notifier.ItemPropertyChanged -= (EventHandler<CollectionItemPropertyChangedEventArgs<T>>)handler;
    }

    public void Dispose() => _notifier.Dispose();
}

/// <summary>
/// Hosts item-level property change notifications for observable collections.
/// </summary>
internal sealed class ObservableCollectionItemPropertyChangeHost<T>
{
    private readonly IEnumerable<T> _items;
    private readonly object _eventOwner;
    private readonly Func<bool> _canRaise;
    private readonly Func<ICollectionEventDispatcher?> _getDispatcher;
    private IItemPropertyChangeSink? _sink;

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
        => EnsureSink().AddHandler(handler);

    public void RemoveHandler(EventHandler<CollectionItemPropertyChangedEventArgs<T>>? handler)
    {
        if (_sink == null) return;

        _sink.RemoveHandler(handler);
        if (_sink.HasHandlers) return;

        _sink.Dispose();
        _sink = null;
    }

    public void HandleCollectionChanged(NotifyCollectionChangedEventArgs e)
        => _sink?.HandleCollectionChanged(e);

    private IItemPropertyChangeSink EnsureSink()
    {
        if (_sink != null) return _sink;

        if (!typeof(INotifyPropertyChanged).IsAssignableFrom(typeof(T)) || typeof(T).IsValueType)
        {
            throw new InvalidOperationException(
                $"Cannot observe item property changes when {typeof(T).Name} does not implement {nameof(INotifyPropertyChanged)} as a reference type.");
        }

        _sink = (IItemPropertyChangeSink)Activator.CreateInstance(
            typeof(ItemPropertyChangeSink<>).MakeGenericType(typeof(T)),
            _items,
            _eventOwner,
            _canRaise,
            _getDispatcher)!;
        return _sink;
    }
}
