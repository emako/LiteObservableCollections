namespace LiteObservableCollections.Extensions;

public static class ObservableListExtensions
{
    /// <summary>
    /// Converts an <see cref="IEnumerable{T}"/> to an <see cref="ObservableList{T}"/>.
    /// </summary>
    public static ObservableList<T> ToObservableList<T>(this IEnumerable<T> collection)
    {
        if (collection == null) throw new ArgumentNullException(nameof(collection));
        return [.. collection];
    }

    /// <summary>
    /// Creates a reactive view over this list with a projection selector.
    /// </summary>
    /// <typeparam name="T">The type of elements in the source list.</typeparam>
    /// <typeparam name="TResult">The type of elements in the view after projection.</typeparam>
    /// <param name="source">The source list.</param>
    /// <param name="selector">The projection function to transform elements.</param>
    /// <returns>A new <see cref="ObservableViewList{T, TResult}"/> that reflects changes from this list.</returns>
    public static ObservableViewList<T, TResult> CreateView<T, TResult>(this ObservableList<T> source, Func<T, TResult> selector)
    {
        return new ObservableViewList<T, TResult>(source, selector);
    }

    /// <summary>
    /// Creates a reactive view over this list without projection.
    /// </summary>
    /// <typeparam name="T">The type of elements in the list.</typeparam>
    /// <param name="source">The source list.</param>
    /// <returns>A new <see cref="ObservableViewList{T}"/> that reflects changes from this list.</returns>
    public static ObservableViewList<T> CreateView<T>(this ObservableList<T> source)
    {
        return new ObservableViewList<T>(source);
    }
}
