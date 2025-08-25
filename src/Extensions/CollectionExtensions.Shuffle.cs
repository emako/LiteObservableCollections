namespace LiteObservableCollections.Extensions;

public static partial class CollectionExtensions
{
    /// <summary>
    /// Reorganizes the collection in a random order and returns it.
    /// </summary>
    public static IList<TSource> Shuffle<TSource>(this IList<TSource> source)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        var n = source.Count;
        while (n > 1)
        {
            n--;
            var k = RandomStore.Shared.Next(n + 1);
            source.Swap(n, k);
        }
        return source;
    }
}
