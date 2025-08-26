using System;
using System.Collections.Generic;

namespace LiteObservableCollections.Extensions;

public static class ObservableQueueExtensions
{
    public static ObservableQueue<T> ToObservableQueue<T>(this IEnumerable<T> collection)
    {
        if (collection == null) throw new ArgumentNullException(nameof(collection));
        var queue = new ObservableQueue<T>();
        foreach (var item in collection)
            queue.Enqueue(item);
        return queue;
    }
}
