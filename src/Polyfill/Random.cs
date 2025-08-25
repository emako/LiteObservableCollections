#pragma warning disable IDE0130 // Namespace does not match folder structure

namespace System;

#pragma warning restore IDE0130 // Namespace does not match folder structure

internal static class RandomStore
{
#if NETSTANDARD || NETFRAMEWORK
    public static Random Shared { get; } = new Random();

#else
    public static Random Shared => Random.Shared;
#endif
}
