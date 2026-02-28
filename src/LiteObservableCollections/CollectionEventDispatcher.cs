using System.Collections.Specialized;
using System.ComponentModel;

namespace LiteObservableCollections;

/// <summary>
/// <see cref="ICollectionEventDispatcher"/> implementation that uses <see cref="SynchronizationContext"/> to marshal
/// notifications to a specific thread (e.g. the UI/dispatcher thread).
/// </summary>
/// <remarks>
/// Creates a dispatcher that posts to the given synchronization context.
/// </remarks>
/// <param name="context">The target context (e.g. from UI thread). If null, <see cref="SynchronizationContext.Current"/> is used at construction time.</param>
public sealed class SynchronizationContextCollectionEventDispatcher(SynchronizationContext? context = null) : ICollectionEventDispatcher
{
    private readonly SynchronizationContext _context = context
        ?? SynchronizationContext.Current
        ?? throw new InvalidOperationException("SynchronizationContext is null. Pass an explicit context (e.g. from the UI thread) or ensure SynchronizationContext.Current is set.");

    /// <inheritdoc />
    public bool IsCurrentContext => SynchronizationContext.Current == _context;

    private static readonly SendOrPostCallback callback = SendOrPostCallback;

    private static readonly Lazy<ICollectionEventDispatcher?> current = new(() =>
    {
        SynchronizationContext? ctx = SynchronizationContext.Current;
        return ctx == null ? null : new SynchronizationContextCollectionEventDispatcher(ctx);
    });

    /// <summary>
    /// Gets a cached dispatcher for the synchronization context that was current when <see cref="Current"/> was first accessed.
    /// </summary>
    /// <remarks>
    /// You must first read <see cref="Current"/> on the target thread (e.g. UI thread during app or window initialization).
    /// That capture happens once; later reads return the same instance. If you cannot guarantee first access on the target thread,
    /// do not use this property — construct a dispatcher explicitly and pass the desired <see cref="SynchronizationContext"/> instead.
    /// </remarks>
    public static ICollectionEventDispatcher? Current => current.Value;

    /// <inheritdoc />
    public void Post(Action action)
    {
        if (action == null) return;
        _context.Post(callback, action);
    }

    /// <inheritdoc />
    public void Send(Action action)
    {
        if (action == null) return;
        _context.Send(callback, action);
    }

    private static void SendOrPostCallback(object? state)
    {
        Action ev = (Action)state!;
        ev.Invoke();
    }
}

public static class SynchronizationContextCollectionEventDispatcherExtension
{
    /// <summary>
    /// Capture in synchronization context of current thread.
    /// The host should be called once on the main/UI thread (e.g. at app startup), after which the library will use dispatch.
    /// </summary>
    /// <seealso cref="SynchronizationContextCollectionEventDispatcher.Current"/> and <seealso cref="SynchronizationContextCollectionEventDispatcher.current"/>.
    public static void CaptureFromCurrentContext(this SynchronizationContextCollectionEventDispatcher self)
    {
        // Must called from the Current property:
        // e.g. `SynchronizationContextCollectionEventDispatcher.Current.CaptureFromCurrentContext`.
        // No-op since context is captured at construction time.
        _ = self;
    }
}

/// <summary>
/// Dispatches collection and property change notifications to a specific synchronization context
/// (e.g. UI thread), avoiding "CollectionView does not support changes to its SourceCollection from a thread other than the dispatcher thread".
/// </summary>
/// <remarks>
/// When set on an observable collection, <see cref="INotifyCollectionChanged"/> and <see cref="INotifyPropertyChanged"/>
/// are marshalled to the dispatcher's context, so the collection can be modified from any thread safely for WPF/Avalonia/WinUI binding.
/// </remarks>
public interface ICollectionEventDispatcher
{
    /// <summary>
    /// Whether the current thread is the dispatcher's target thread. When true, events can be raised directly without posting.
    /// </summary>
    public bool IsCurrentContext { get; }

    /// <summary>
    /// Posts an action to be executed on the dispatcher's target context (e.g. UI thread).
    /// </summary>
    /// <param name="action">The action to run on the target context.</param>
    public void Post(Action action);

    /// <summary>
    /// Sends an action to be executed synchronously on the dispatcher's target context. Reserved for future use.
    /// </summary>
    /// <param name="action">The action to run on the target context.</param>
    public void Send(Action action);
}
