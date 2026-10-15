using LiteObservableCollections.EventListeners;

namespace LiteObservableCollections;

/// <summary>
/// Exposes item-level <see cref="System.ComponentModel.INotifyPropertyChanged"/> notifications for items in a collection.
/// </summary>
/// <typeparam name="T">The type of items being observed.</typeparam>
/// <remarks>
/// Inherited by <see cref="IObservableList{T}"/> and <see cref="IObservableCollection{T}"/>, and implemented by
/// <see cref="CollectionItemPropertyChangedListener{T}"/>.
/// Reference-type items that implement <see cref="System.ComponentModel.INotifyPropertyChanged"/> are observed;
/// other items are ignored.
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
    public event EventHandler<ItemPropertyChangedEventArgs<T>>? ItemPropertyChanged;
}
