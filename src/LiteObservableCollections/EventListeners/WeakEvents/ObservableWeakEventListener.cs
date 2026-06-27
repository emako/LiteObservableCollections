namespace LiteObservableCollections.EventListeners.WeakEvents;

/// <summary>
/// Generic WeakEvent listener object.
/// </summary>
/// <typeparam name="THandler">Type of event handler for the target event</typeparam>
/// <typeparam name="TEventArgs">Type of event arguments for the target event</typeparam>
public class ObservableWeakEventListener<THandler, TEventArgs> : IDisposable where TEventArgs : EventArgs
{
    private readonly bool _initialized;
    private bool _disposed;

    private EventHandler<TEventArgs>? _handler;
    private Action<THandler>? _remove;
    private THandler? _resultHandler;

    protected ObservableWeakEventListener()
    {
    }

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="conversion">Func to convert the generic event handler type to THandler</param>
    /// <param name="add">Action to subscribe to the event (e.g. h => obj.Event += h). h is of type THandler.</param>
    /// <param name="remove">Action to unsubscribe from the event (e.g. h => obj.Event -= h). h is of type THandler.</param>
    /// <param name="handler">Action to perform when the event is received</param>
    public ObservableWeakEventListener(Func<EventHandler<TEventArgs>, THandler> conversion,
        Action<THandler> add,
        Action<THandler> remove, EventHandler<TEventArgs> handler)
    {
        if (conversion == null) throw new ArgumentNullException(nameof(conversion));
        if (add == null) throw new ArgumentNullException(nameof(add));
        if (remove == null) throw new ArgumentNullException(nameof(remove));
        if (handler == null) throw new ArgumentNullException(nameof(handler));

        Initialize(conversion, add, remove, handler);
        _initialized = true;
    }

    /// <summary>
    /// Disconnects from the event source.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private static void ReceiveEvent(
        WeakReference<ObservableWeakEventListener<THandler, TEventArgs>> listenerWeakReference, object? sender,
        TEventArgs args)
    {
        if (listenerWeakReference == null) throw new ArgumentNullException(nameof(listenerWeakReference));

        if (listenerWeakReference.TryGetTarget(out var listenerResult))
            listenerResult?._handler?.Invoke(sender, args);
    }

    private static THandler GetStaticHandler(
        WeakReference<ObservableWeakEventListener<THandler, TEventArgs>> listenerWeakReference,
        Func<EventHandler<TEventArgs>, THandler> conversion)
    {
        if (listenerWeakReference == null) throw new ArgumentNullException(nameof(listenerWeakReference));
        if (conversion == null) throw new ArgumentNullException(nameof(conversion));

        return conversion.Invoke((sender, e) => ReceiveEvent(listenerWeakReference, sender, e));
    }

    protected void Initialize(Func<EventHandler<TEventArgs>, THandler> conversion,
        Action<THandler> add,
        Action<THandler> remove, EventHandler<TEventArgs> handler)
    {
        if (_initialized) return;

        if (conversion == null) throw new ArgumentNullException(nameof(conversion));
        if (add == null) throw new ArgumentNullException(nameof(add));

        _handler = handler ?? throw new ArgumentNullException(nameof(handler));
        _remove = remove ?? throw new ArgumentNullException(nameof(remove));

        _resultHandler = GetStaticHandler(new WeakReference<ObservableWeakEventListener<THandler, TEventArgs>>(this),
            conversion);

        add(_resultHandler);
    }

    protected void ThrowExceptionIfDisposed()
    {
        if (_disposed) throw new ObjectDisposedException("ObservableWeakEventListener");
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;

        if (disposing)
        {
            _remove?.Invoke(_resultHandler!);
            _handler = null;
            _resultHandler = default;
            _remove = null;
        }

        _disposed = true;
    }
}
