using LiteObservableCollections.EventListeners.Internals;
using System.Collections;
using System.Collections.Specialized;

namespace LiteObservableCollections.EventListeners.WeakEvents;

/// <summary>
/// Weak event listener for receiving INotifyCollectionChanged.NotifyCollectionChanged events.
/// </summary>
public sealed class CollectionChangedWeakEventListener :
    ObservableWeakEventListener<NotifyCollectionChangedEventHandler, NotifyCollectionChangedEventArgs>,
    IEnumerable<KeyValuePair<NotifyCollectionChangedAction, List<NotifyCollectionChangedEventHandler>>>
{
    private readonly AnonymousCollectionChangedEventHandlerBag _bag;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="source">INotifyCollectionChanged object</param>
    public CollectionChangedWeakEventListener(INotifyCollectionChanged source)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));

        _bag = new AnonymousCollectionChangedEventHandlerBag(source);
        Initialize(
            h => new NotifyCollectionChangedEventHandler(h ?? throw new ArgumentNullException(nameof(h))),
            h => source.CollectionChanged += h,
            h => source.CollectionChanged -= h,
            (sender, e) => _bag.ExecuteHandler(e ?? throw new ArgumentNullException(nameof(e))));
    }

    /// <summary>
    /// Constructor. Registers one handler at the same time as creating the listener instance.
    /// </summary>
    /// <param name="source">INotifyCollectionChanged object</param>
    /// <param name="handler">NotifyCollectionChanged event handler</param>
    public CollectionChangedWeakEventListener(INotifyCollectionChanged source,
        NotifyCollectionChangedEventHandler handler)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (handler == null) throw new ArgumentNullException(nameof(handler));

        _bag = new AnonymousCollectionChangedEventHandlerBag(source, handler);
        Initialize(
            h => new NotifyCollectionChangedEventHandler(h ?? throw new ArgumentNullException(nameof(h))),
            h => source.CollectionChanged += h,
            h => source.CollectionChanged -= h,
            (sender, e) => _bag.ExecuteHandler(e ?? throw new ArgumentNullException(nameof(e))));
    }

    IEnumerator<KeyValuePair<NotifyCollectionChangedAction, List<NotifyCollectionChangedEventHandler>>>
        IEnumerable<KeyValuePair<NotifyCollectionChangedAction, List<NotifyCollectionChangedEventHandler>>>.
        GetEnumerator()
    {
        return
            ((
                    IEnumerable
                    <KeyValuePair<NotifyCollectionChangedAction, List<NotifyCollectionChangedEventHandler>>>)
                _bag).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((
                IEnumerable
                <KeyValuePair<NotifyCollectionChangedAction, List<NotifyCollectionChangedEventHandler>>>)
            _bag).GetEnumerator();
    }

    /// <summary>
    /// Adds a new handler to this listener instance.
    /// </summary>
    /// <param name="handler">NotifyCollectionChanged event handler</param>
    public void RegisterHandler(NotifyCollectionChangedEventHandler handler)
    {
        ThrowExceptionIfDisposed();
        _bag.RegisterHandler(handler);
    }

    /// <summary>
    /// Adds a handler filtered by action to this listener instance.
    /// </summary>
    /// <param name="action">NotifyCollectionChangedAction to register the handler for</param>
    /// <param name="handler">NotifyCollectionChanged event handler corresponding to the specified action</param>
    public void RegisterHandler(NotifyCollectionChangedAction action, NotifyCollectionChangedEventHandler handler)
    {
        ThrowExceptionIfDisposed();
        _bag.RegisterHandler(action, handler);
    }

    public void Add(NotifyCollectionChangedEventHandler handler)
    {
        _bag.Add(handler);
    }

    public void Add(NotifyCollectionChangedAction action, NotifyCollectionChangedEventHandler handler)
    {
        _bag.Add(action, handler);
    }

    public void Add(NotifyCollectionChangedAction action,
        params NotifyCollectionChangedEventHandler[] handlers)
    {
        if (handlers == null) throw new ArgumentNullException(nameof(handlers));

        _bag.Add(action, handlers);
    }
}
