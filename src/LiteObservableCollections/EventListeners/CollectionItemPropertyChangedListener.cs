using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace LiteObservableCollections.EventListeners;

/// <summary>
/// Observes <see cref="INotifyPropertyChanged.PropertyChanged"/> events from every item in an observable collection.
/// Subscriptions are automatically maintained as items are added, removed, replaced, or reset.
/// </summary>
/// <typeparam name="T">The reference type of items to observe.</typeparam>
public sealed class CollectionItemPropertyChangedListener<T> : IDisposable where T : class, INotifyPropertyChanged
{
    private readonly INotifyCollectionChanged _source;
    private readonly IEnumerable<T> _items;
    private readonly Dictionary<T, int> _subscriptionCounts = [with(ReferenceComparer.Instance)];
    private bool _disposed;

    /// <summary>
    /// Initializes a listener for the specified observable collection.
    /// </summary>
    /// <param name="source">The collection whose items will be observed.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="source"/> does not implement both required interfaces.</exception>
    public CollectionItemPropertyChangedListener(IEnumerable<T> source)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (source is not INotifyCollectionChanged observableSource)
            throw new ArgumentException("The source must implement INotifyCollectionChanged.", nameof(source));

        _items = source;
        _source = observableSource;

        foreach (T item in _items)
            Subscribe(item);

        _source.CollectionChanged += OnCollectionChanged;
    }

    /// <summary>
    /// Occurs when a property changes on an item currently contained in the source collection.
    /// </summary>
    public event EventHandler<CollectionItemPropertyChangedEventArgs<T>>? ItemPropertyChanged;

    /// <summary>
    /// Unsubscribes from the collection and all currently observed items.
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;

        _source.CollectionChanged -= OnCollectionChanged;
        foreach (T item in _subscriptionCounts.Keys.ToArray())
            item.PropertyChanged -= OnItemPropertyChanged;

        _subscriptionCounts.Clear();
        _disposed = true;
        GC.SuppressFinalize(this);
    }

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
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

    private void OnItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is T item && !_disposed)
            ItemPropertyChanged?.Invoke(this, new CollectionItemPropertyChangedEventArgs<T>(item, e));
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

/// <summary>
/// Provides the item and property-change details for <see cref="CollectionItemPropertyChangedListener{T}.ItemPropertyChanged"/>.
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
