using System;
using System.Collections.Generic;

namespace LiteObservableCollections.Extensions;

public static class ObservableHashSetExtensions
{
    public static ObservableHashSet<T> ToObservableHashSet<T>(this IEnumerable<T> collection)
    {
        if (collection == null) throw new ArgumentNullException(nameof(collection));
        return new ObservableHashSet<T>(collection);
    }
}
