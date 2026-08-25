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
    private readonly Dictionary<T, int> _subscriptionCounts = [with(ReferenceComparer.Instance)];
    private bool _disposed;

    public CollectionItemPropertyChangeNotifier(IEnumerable<T> items)
    {
        _items = items ?? throw new ArgumentNullException(nameof(items));

        foreach (T item in _items)
            Subscribe(item);
    }

    public event EventHandler<CollectionItemPropertyChangedEventArgs<T>>? ItemPropertyChanged;

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
        _disposed = true;
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
