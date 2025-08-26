using System;
using System.Collections.Generic;

namespace LiteObservableCollections.Extensions;

public static class ObservableDictionaryExtensions
{
    public static ObservableDictionary<TKey, TValue> ToObservableDictionary<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> collection)
    {
        if (collection == null) throw new ArgumentNullException(nameof(collection));
        var dict = new ObservableDictionary<TKey, TValue>();
        foreach (var kv in collection)
            dict.Add(kv.Key, kv.Value);
        return dict;
    }
}
