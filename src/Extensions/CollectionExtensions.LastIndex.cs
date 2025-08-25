namespace LiteObservableCollections.Extensions;

public static partial class CollectionExtensions
{
    /// <summary>
    /// Returns the last index of the collection.
    /// </summary>
    public static int LastIndex<TSource>(this IEnumerable<TSource> source)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        var list = source as IList<TSource> ?? [.. source];
        return list.Count - 1;
    }
}
