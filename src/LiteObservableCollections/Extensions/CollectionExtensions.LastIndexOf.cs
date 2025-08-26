namespace LiteObservableCollections.Extensions;

public static partial class CollectionExtensions
{
    public static int LastIndexOf<TSource>(this IEnumerable<TSource> source, TSource? item) => source.LastIndexOf(x => Equals(x, item));

    public static int LastIndexOf<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> match)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (match == null) throw new ArgumentNullException(nameof(match));

        var list = source as IList<TSource> ?? [.. source];

        for (var i = list.Count - 1; i >= 0; i--)
            if (match(list[i]))
                return i;

        return -1;
    }
}
