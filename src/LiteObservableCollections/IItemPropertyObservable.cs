using LiteObservableCollections.EventListeners;

namespace LiteObservableCollections;

/// <summary>
/// Exposes item-level <see cref="System.ComponentModel.INotifyPropertyChanged"/> notifications for items in a collection.
/// </summary>
/// <typeparam name="T">The type of items being observed.</typeparam>
/// <remarks>
/// Implemented by <see cref="ObservableList{T}"/>, <see cref="ObservableCollection{T}"/>, and
/// <see cref="CollectionItemPropertyChangedListener{T}"/>. Prefer this interface over downcasting to a concrete collection type.
/// Built-in collection events observe reference-type items that implement
/// <see cref="System.ComponentModel.INotifyPropertyChanged"/>; other items are ignored.
/// </remarks>
public interface IItemPropertyObservable<T>
{
    /// <summary>
    /// Occurs when a property changes on an item currently contained in the source.
    /// </summary>
    /// <remarks>
    /// Raised for item property changes only. For add, remove, replace, and reset notifications, use
    /// <see cref="System.Collections.Specialized.INotifyCollectionChanged.CollectionChanged"/>.
    /// </remarks>
    public event EventHandler<CollectionItemPropertyChangedEventArgs<T>>? ItemPropertyChanged;
}
