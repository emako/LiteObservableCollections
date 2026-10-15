using LiteObservableCollections.EventListeners;

namespace LiteObservableCollections.Internals;

/// <summary>
/// Lets <see cref="CollectionItemPropertyChangedListener{T}"/> reuse a built-in collection's item subscriptions,
/// which stay synchronized even when the collection suppresses its outward notifications.
/// </summary>
/// <remarks>
/// Direct handlers are raised synchronously on the thread that raised the item's property change and ignore
/// <c>IsNotifyEnabled</c> and <c>EventDispatcher</c>.
/// </remarks>
internal interface IDirectItemPropertyChangeSource<T>
{
    public void AddDirectItemPropertyChangedHandler(EventHandler<ItemPropertyChangedEventArgs<T>> handler);

    public void RemoveDirectItemPropertyChangedHandler(EventHandler<ItemPropertyChangedEventArgs<T>> handler);
}
