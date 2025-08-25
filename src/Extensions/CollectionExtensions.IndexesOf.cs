namespace LiteObservableCollections.Extensions;

public static partial class CollectionExtensions
{
    public static IReadOnlyList<int> IndexesOf<TSource>(this IEnumerable<TSource> source, TSource? item) => source.IndexesOf(x => Equals(x, item));

    public static IReadOnlyList<int> IndexesOf<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> match)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (match == null) throw new ArgumentNullException(nameof(match));
        var list = source as IList<TSource> ?? [.. source];

        if (list.Count == 0)
            return Array.Empty<int>();

        var output = new List<int>();
        for (var i = 0; i < list.Count; i++)
            if (match(list[i]))
                output.Add(i);

        return output;
    }
}
