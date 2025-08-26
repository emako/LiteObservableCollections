using System;
using System.Collections.Generic;

namespace LiteObservableCollections.Extensions;

public static class ObservableStackExtensions
{
    public static ObservableStack<T> ToObservableStack<T>(this IEnumerable<T> collection)
    {
        if (collection == null) throw new ArgumentNullException(nameof(collection));
        var stack = new ObservableStack<T>();
        foreach (var item in collection)
            stack.Push(item);
        return stack;
    }
}
