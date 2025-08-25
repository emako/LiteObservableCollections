namespace LiteObservableCollections;

/// <summary>
/// An observable, dynamic one-dimensional array.
/// </summary>
public interface IObservableList<T> : IList<T>, IReadOnlyList<T>, IObservableCollection<T>
{
    /// <summary>
    /// Number of elements contained in the collection.
    /// </summary>
    public new int Count { get; }

    public new T this[int index] { get; set; }

    /// <summary>
    /// Index of the last element in the collection.
    /// </summary>
    public int LastIndex { get; }

    public void TryRemoveFirst(T item);

    public void RemoveFirst(T item);

    public void TryRemoveFirst(Func<T, bool> predicate);

    public void RemoveFirst(Func<T, bool> predicate);

    public void TryRemoveLast(T item);

    public void RemoveLast(T item);

    public void TryRemoveLast(Func<T, bool> predicate);

    public void RemoveLast(Func<T, bool> predicate);

    public void TryRemoveAll(T item);

    public void RemoveAll(T item);

    public void RemoveAll(params T[] items);

    public void RemoveAll(IEnumerable<T> items);

    public void TryRemoveAll(params T[] items);

    public void TryRemoveAll(IEnumerable<T> items);

    public void TryRemoveAll(Func<T, bool> predicate);

    public void RemoveAll(Func<T, bool> predicate);

    public void RemoveAt(int index, int count);

    public void Add(params T[]? items);

    public void Add(IEnumerable<T> items);

    /// <summary>
    /// Inserts items at the beginning of ObservableList.
    /// </summary>
    public void Insert(params T[] items);

    /// <summary>
    /// Inserts items at the beginning of ObservableList.
    /// </summary>
    public void Insert(IEnumerable<T> items);

    public void Insert(int index, params T[] items);

    public void Insert(int index, IEnumerable<T> items);

    public ObservableList<T> Copy(int startingIndex = 0);

    ObservableList<T> Copy(int startingIndex, int count);

    public void Swap(int currentIndex, int destinationIndex);

    /// <summary>
    /// Resizes the collection down to <see cref="maxSize"/> by prioritizing keeping the last elements.
    /// </summary>
    public void TrimStartDownTo(int maxSize);

    /// <summary>
    /// Resizes the collection down to <see cref="maxSize"/> by prioritizing keeping the first elements.
    /// </summary>
    public void TrimEndDownTo(int maxSize);

    /// <summary>
    /// Randomly rearranges the collection's content order.
    /// </summary>
    public IObservableList<T> Shuffle();

    /// <summary>
    /// Clears the collection and adds those items instead.
    /// </summary>
    public void Overwrite(params T[]? items);

    /// <summary>
    /// Clears the collection and adds those items instead.
    /// </summary>
    public void Overwrite(IEnumerable<T> items);

    /// <summary>
    /// Reverses the order of the elements in the entire <see cref="IObservableList{T}"/>.
    /// </summary>
    public void Reverse();

    /// <summary>
    /// Reverses the order of the elements in the entire <see cref="IObservableList{T}"/>.
    /// </summary>
    public void Reverse(int index, int count);
}
