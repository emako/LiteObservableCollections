using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;

namespace LiteObservableCollections;

/// <summary>
/// A reactive view over an <see cref="ObservableList{T}"/> that supports filtering and projection.
/// Changes in the source collection are automatically reflected in the view.
/// </summary>
/// <typeparam name="TSource">The type of elements in the source collection.</typeparam>
/// <typeparam name="TResult">The type of elements in the view after projection.</typeparam>
public class ObservableViewList<TSource, TResult> : IReadOnlyList<TResult>, INotifyCollectionChanged, INotifyPropertyChanged, IDisposable
{
    private readonly ObservableList<TSource> _source;
    private readonly Func<TSource, TResult> _selector;
    private readonly List<TResult> _view = [];
    private Func<TSource, bool>? _filter;
    private Comparison<TResult>? _sortComparison;
    private bool _disposed;

    /// <summary>
    /// Initializes a new view over the specified source collection with a projection selector.
    /// </summary>
    /// <param name="source">The source collection to create a view over.</param>
    /// <param name="selector">The projection function to transform source elements.</param>
    public ObservableViewList(ObservableList<TSource> source, Func<TSource, TResult> selector)
    {
        _source = source ?? throw new ArgumentNullException(nameof(source));
        _selector = selector ?? throw new ArgumentNullException(nameof(selector));
        _source.CollectionChanged += OnSourceCollectionChanged;
        Refresh();
    }

    /// <summary>
    /// Occurs when the view collection changes.
    /// </summary>
    public event NotifyCollectionChangedEventHandler? CollectionChanged;

    /// <summary>
    /// Occurs when a property value changes.
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Gets the element at the specified index.
    /// </summary>
    public TResult this[int index] => _view[index];

    /// <summary>
    /// Gets the number of elements in the view.
    /// </summary>
    public int Count => _view.Count;

    /// <summary>
    /// Gets the current filter predicate. Returns null if no filter is attached.
    /// </summary>
    public Func<TSource, bool>? Filter => _filter;

    /// <summary>
    /// Gets the current sort comparison. Returns null if the view is in source order (no sort applied).
    /// </summary>
    public Comparison<TResult>? SortComparison => _sortComparison;

    /// <summary>
    /// Attaches a filter predicate to the view. Items not matching the predicate are excluded.
    /// The filter operates on source elements (before projection).
    /// </summary>
    /// <param name="predicate">The filter predicate.</param>
    public void AttachFilter(Func<TSource, bool> predicate)
    {
        _filter = predicate ?? throw new ArgumentNullException(nameof(predicate));
        Refresh();
    }

    /// <summary>
    /// Removes the filter, showing all items from the source collection.
    /// </summary>
    public void ResetFilter()
    {
        _filter = null;
        Refresh();
    }

    /// <summary>
    /// Forces a full refresh of the view.
    /// </summary>
    public void Refresh()
    {
        _view.Clear();

        IEnumerable<TSource> items = _source;

        if (_filter != null)
            items = items.Where(_filter);

        foreach (TSource item in items)
            _view.Add(_selector(item));

        if (_sortComparison != null)
            _view.Sort(_sortComparison);

        OnPropertyChanged(nameof(Count));
        CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }

    /// <summary>
    /// Returns an enumerator that iterates through the view.
    /// </summary>
    public IEnumerator<TResult> GetEnumerator() => _view.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => _view.GetEnumerator();

    /// <summary>
    /// Sorts the elements in the view using the default comparer.
    /// The sort is persisted: <see cref="Refresh"/> will re-apply it. Use <see cref="ResetSort"/> to restore source order.
    /// </summary>
    public void AttachSort()
    {
        _sortComparison = (a, b) => Comparer<TResult>.Default.Compare(a!, b!);
        _view.Sort();
        CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }

    /// <summary>
    /// Sorts the elements in the view using the specified comparer.
    /// The sort is persisted: <see cref="Refresh"/> will re-apply it. Use <see cref="ResetSort"/> to restore source order.
    /// </summary>
    /// <param name="comparer">The comparer to use when comparing elements.</param>
    public void AttachSort(IComparer<TResult> comparer)
    {
        _sortComparison = comparer.Compare;
        _view.Sort(comparer);
        CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }

    /// <summary>
    /// Sorts the elements in the view using the specified comparison.
    /// The sort is persisted: <see cref="Refresh"/> will re-apply it. Use <see cref="ResetSort"/> to restore source order.
    /// </summary>
    /// <param name="comparison">The comparison to use when comparing elements.</param>
    public void AttachSort(Comparison<TResult> comparison)
    {
        _sortComparison = comparison;
        _view.Sort(comparison);
        CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }

    /// <summary>
    /// Clears the current sort and restores the view order to match the source collection (and re-applies the current filter).
    /// </summary>
    public void ResetSort()
    {
        _sortComparison = null;
        Refresh();
    }

    private void OnSourceCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // Refresh view automatically when source collection changes
        Refresh();
    }

    private void OnPropertyChanged(string propertyName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    /// <summary>
    /// Releases resources and unsubscribes from the source collection.
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _source.CollectionChanged -= OnSourceCollectionChanged;
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// A reactive view over an <see cref="ObservableList{T}"/> that supports filtering without projection.
/// Changes in the source collection are automatically reflected in the view.
/// </summary>
/// <typeparam name="T">The type of elements in the collection.</typeparam>
/// <remarks>
/// Initializes a new view over the specified source collection.
/// </remarks>
/// <param name="source">The source collection to create a view over.</param>
public class ObservableViewList<T>(ObservableList<T> source) : ObservableViewList<T, T>(source, x => x);
