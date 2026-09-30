using System.Collections.Specialized;
using System.ComponentModel;
using LiteObservableCollections.Internals;

namespace LiteObservableCollections.EventListeners;

/// <summary>
/// Observes <see cref="INotifyPropertyChanged.PropertyChanged"/> events from every item in an observable collection.
/// Subscriptions are automatically maintained as items are added, removed, replaced, or reset.
/// </summary>
/// <typeparam name="T">The reference type of items to observe.</typeparam>
public sealed class CollectionItemPropertyChangedListener<T> : IItemPropertyObservable<T>, IDisposable
    where T : class, INotifyPropertyChanged
{
    private readonly INotifyCollectionChanged _source;
    private readonly CollectionItemPropertyChangeNotifier<T> _notifier;
    private bool _disposed;

    /// <summary>
    /// Initializes a listener for the specified observable collection.
    /// </summary>
    /// <param name="source">
    /// The collection whose items will be observed. Must implement both <see cref="IEnumerable{T}"/> and
    /// <see cref="INotifyCollectionChanged"/>.
    /// </param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="source"/> does not implement both required interfaces.</exception>
    public CollectionItemPropertyChangedListener(IEnumerable<T> source)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (source is not INotifyCollectionChanged observableSource)
            throw new ArgumentException("The source must implement INotifyCollectionChanged.", nameof(source));

        _notifier = new CollectionItemPropertyChangeNotifier<T>(source, this);
        _source = observableSource;
        _source.CollectionChanged += OnCollectionChanged;
    }

    /// <summary>
    /// Occurs when a property changes on an item currently contained in the source collection.
    /// </summary>
    /// <remarks>
    /// This event is raised for item property changes only. For add, remove, replace, and reset notifications, subscribe to the source collection's <see cref="INotifyCollectionChanged.CollectionChanged"/> event or use <see cref="CollectionChangedEventListener"/>.
    /// The event <c>sender</c> is this listener instance.
    /// </remarks>
    public event EventHandler<CollectionItemPropertyChangedEventArgs<T>>? ItemPropertyChanged
    {
        add => _notifier.ItemPropertyChanged += value;
        remove => _notifier.ItemPropertyChanged -= value;
    }

    /// <summary>
    /// Unsubscribes from the collection and all currently observed items.
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;

        _source.CollectionChanged -= OnCollectionChanged;
        _notifier.Dispose();
        _disposed = true;
        GC.SuppressFinalize(this);
    }

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (!_disposed)
            _notifier.HandleCollectionChanged(e);
    }
}

/// <summary>
/// Provides the item and property-change details for collection item property change events.
/// </summary>
/// <typeparam name="T">The type of the changed item.</typeparam>
/// <remarks>
/// Initializes event arguments for a changed collection item.
/// </remarks>
/// <param name="item">The item whose property changed.</param>
/// <param name="propertyChangedEventArgs">The original property-change event arguments.</param>
public sealed class CollectionItemPropertyChangedEventArgs<T>(T item, PropertyChangedEventArgs propertyChangedEventArgs) : EventArgs
{
    /// <summary>
    /// Gets the item whose property changed.
    /// </summary>
    public T Item { get; } = item;

    /// <summary>
    /// Gets the original property-change event arguments.
    /// </summary>
    public PropertyChangedEventArgs PropertyChangedEventArgs { get; } = propertyChangedEventArgs ?? throw new ArgumentNullException(nameof(propertyChangedEventArgs));
}
