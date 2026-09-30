using System.ComponentModel;

namespace LiteObservableCollections;

/// <summary>
/// Provides the item and property-change details for <see cref="IItemPropertyObservable{T}.ItemPropertyChanged"/>.
/// </summary>
/// <typeparam name="T">The type of the changed item.</typeparam>
/// <remarks>
/// Initializes event arguments for a changed collection item.
/// </remarks>
/// <param name="item">The item whose property changed.</param>
/// <param name="propertyChangedEventArgs">The original property-change event arguments.</param>
public sealed class ItemPropertyChangedEventArgs<T>(T item, PropertyChangedEventArgs propertyChangedEventArgs) : EventArgs
{
    /// <summary>
    /// Gets the item whose property changed.
    /// </summary>
    public T Item { get; } = item;

    /// <summary>
    /// Gets the original property-change event arguments.
    /// </summary>
    public PropertyChangedEventArgs PropertyChangedEventArgs { get; } = propertyChangedEventArgs ?? throw new ArgumentNullException(nameof(propertyChangedEventArgs));

    /// <summary>
    /// Gets the name of the property that changed, or <see langword="null"/> or empty when all properties changed.
    /// </summary>
    public string? PropertyName => PropertyChangedEventArgs.PropertyName;
}
