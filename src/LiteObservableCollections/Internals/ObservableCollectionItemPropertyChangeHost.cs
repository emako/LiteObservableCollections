using System.Collections.Specialized;
using System.ComponentModel;
using LiteObservableCollections.EventListeners;

namespace LiteObservableCollections.Internals;

internal interface IItemPropertyChangeSink
{
    void HandleCollectionChanged(NotifyCollectionChangedEventArgs e);

    void AddHandler(Delegate handler);

    void RemoveHandler(Delegate? handler);
}

internal sealed class ItemPropertyChangeSink<T> : IItemPropertyChangeSink where T : class, INotifyPropertyChanged
{
    private readonly CollectionItemPropertyChangeNotifier<T> _notifier;

    public ItemPropertyChangeSink(IEnumerable<T> items)
        => _notifier = new CollectionItemPropertyChangeNotifier<T>(items);

    public void HandleCollectionChanged(NotifyCollectionChangedEventArgs e)
        => _notifier.HandleCollectionChanged(e);

    public void AddHandler(Delegate handler)
        => _notifier.ItemPropertyChanged += (EventHandler<CollectionItemPropertyChangedEventArgs<T>>)handler;

    public void RemoveHandler(Delegate? handler)
    {
        if (handler != null)
            _notifier.ItemPropertyChanged -= (EventHandler<CollectionItemPropertyChangedEventArgs<T>>)handler;
    }
}

/// <summary>
/// Hosts item-level property change notifications for observable collections.
/// </summary>
internal sealed class ObservableCollectionItemPropertyChangeHost<T>
{
    private readonly IEnumerable<T> _items;
    private IItemPropertyChangeSink? _sink;

    public ObservableCollectionItemPropertyChangeHost(IEnumerable<T> items)
        => _items = items ?? throw new ArgumentNullException(nameof(items));

    public void AddHandler(EventHandler<CollectionItemPropertyChangedEventArgs<T>> handler)
        => EnsureSink().AddHandler(handler);

    public void RemoveHandler(EventHandler<CollectionItemPropertyChangedEventArgs<T>>? handler)
        => _sink?.RemoveHandler(handler);

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
            typeof(ItemPropertyChangeSink<>).MakeGenericType(typeof(T)), _items)!;
        return _sink;
    }
}
