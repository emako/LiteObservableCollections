using System.Collections.Specialized;
using System.ComponentModel;
using LiteObservableCollections.Internals;

namespace LiteObservableCollections.EventListeners;

/// <summary>
/// Observes <see cref="INotifyPropertyChanged.PropertyChanged"/> events from every item in an observable collection.
/// Subscriptions are automatically maintained as items are added, removed, replaced, or reset.
/// </summary>
/// <typeparam name="T">
/// The type of items in the source. Reference-type items that implement <see cref="INotifyPropertyChanged"/> are observed;
/// other items are ignored.
/// </typeparam>
/// <remarks>
/// <see cref="ItemPropertyChanged"/> is raised synchronously on the thread that raised the item's property change.
/// It is not affected by the source's <c>IsNotifyEnabled</c> or <c>EventDispatcher</c>.
/// </remarks>
public sealed class CollectionItemPropertyChangedListener<T> : IItemPropertyObservable<T>, IDisposable
{
    private readonly INotifyCollectionChanged _source;
    private readonly IDirectItemPropertyChangeSource<T>? _directSource;
    private readonly CollectionItemPropertyChangeNotifier<T>? _notifier;
    private volatile bool _disposed;

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

        _source = observableSource;

        if (source is IDirectItemPropertyChangeSource<T> directSource)
        {
            _directSource = directSource;
            _directSource.AddDirectItemPropertyChangedHandler(OnItemPropertyChanged);
        }
        else
        {
            _notifier = new CollectionItemPropertyChangeNotifier<T>(source, this);
            _notifier.DirectItemPropertyChanged += OnItemPropertyChanged;
            _source.CollectionChanged += OnCollectionChanged;
        }
    }

    /// <summary>
    /// Occurs when a property changes on an item currently contained in the source collection.
    /// </summary>
    /// <remarks>
    /// This event is raised for item property changes only. For add, remove, replace, and reset notifications, subscribe to the source collection's <see cref="INotifyCollectionChanged.CollectionChanged"/> event or use <see cref="CollectionChangedEventListener"/>.
    /// The event <c>sender</c> is this listener instance.
    /// </remarks>
    public event EventHandler<ItemPropertyChangedEventArgs<T>>? ItemPropertyChanged;

    /// <summary>
    /// Unsubscribes from the collection and all currently observed items.
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        if (_directSource != null)
        {
            _directSource.RemoveDirectItemPropertyChangedHandler(OnItemPropertyChanged);
        }
        else
        {
            _source.CollectionChanged -= OnCollectionChanged;
            _notifier!.Dispose();
        }

        GC.SuppressFinalize(this);
    }

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (!_disposed)
            _notifier!.HandleCollectionChanged(e);
    }

    private void OnItemPropertyChanged(object? sender, ItemPropertyChangedEventArgs<T> e)
    {
        if (!_disposed)
            ItemPropertyChanged?.Invoke(this, e);
    }
}
