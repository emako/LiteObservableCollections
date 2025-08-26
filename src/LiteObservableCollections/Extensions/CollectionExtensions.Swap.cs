namespace LiteObservableCollections.Extensions;

public static partial class CollectionExtensions
{
    public static void Swap<TSource>(this IList<TSource> source, int currentIndex, int destinationIndex)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (currentIndex < 0 || currentIndex > source.LastIndex()) throw new ArgumentOutOfRangeException(nameof(currentIndex));
        if (destinationIndex < 0 || destinationIndex > source.LastIndex()) throw new ArgumentOutOfRangeException(nameof(destinationIndex));

        var currentItem = source[currentIndex];
        var destinationItem = source[destinationIndex];
        source[currentIndex] = destinationItem;
        source[destinationIndex] = currentItem;
    }
}
