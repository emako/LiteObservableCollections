namespace LiteObservableCollections.Extensions;

public static partial class CollectionExtensions
{
    /// <summary>
    /// Removes and returns the first item from collection.
    /// </summary>
    public static int FirstIndexOf<TSource>(this IEnumerable<TSource> source, TSource? item) => source.FirstIndexOf(x => Equals(x, item));

    public static int FirstIndexOf<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> match)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (match == null) throw new ArgumentNullException(nameof(match));
        var list = source as IList<TSource> ?? [.. source];

        for (var i = 0; i < list.Count; i++)
            if (match(list[i]))
                return i;

        return -1;
    }
}
