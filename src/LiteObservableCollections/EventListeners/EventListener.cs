namespace LiteObservableCollections.EventListeners;

/// <summary>
/// Generic event listener object.
/// </summary>
/// <typeparam name="THandler">Type of event handler</typeparam>
public class EventListener<THandler> : IDisposable where THandler : class
{
    private readonly bool _initialized;

    // ReSharper disable once NotAccessedField.Local
    private Action<THandler>? _add;

    private bool _disposed;
    private THandler? _handler;
    private Action<THandler>? _remove;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="add">Action to subscribe to the event (e.g. h => obj.Event += h). h is of type THandler.</param>
    /// <param name="remove">Action to unsubscribe from the event (e.g. h => obj.Event -= h). h is of type THandler.</param>
    /// <param name="handler">Action to perform when the event is received</param>
    public EventListener(Action<THandler> add, Action<THandler> remove,
        THandler handler)
    {
        if (add == null) throw new ArgumentNullException(nameof(add));
        if (remove == null) throw new ArgumentNullException(nameof(remove));
        if (handler == null) throw new ArgumentNullException(nameof(handler));

        Initialize(add, remove, handler);
        _initialized = true;
    }

    protected EventListener()
    {
    }

    /// <summary>
    /// Unregisters the event handler.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected void Initialize(Action<THandler> add, Action<THandler> remove,
        THandler handler)
    {
        if (_initialized) return;

        _add = add ?? throw new ArgumentNullException(nameof(add));
        _handler = handler ?? throw new ArgumentNullException(nameof(handler));
        _remove = remove ?? throw new ArgumentNullException(nameof(remove));
        add(handler);
    }

    protected void ThrowExceptionIfDisposed()
    {
        if (_disposed) throw new ObjectDisposedException("EventListener");
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;

        if (disposing)
        {
            _remove?.Invoke(_handler!);
            _add = null;
            _remove = null;
            _handler = null;
        }

        _disposed = true;
    }
}
