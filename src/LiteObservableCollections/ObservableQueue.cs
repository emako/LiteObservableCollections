using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;

namespace LiteObservableCollections;

/// <summary>
/// A lite observable queue that supports <see cref="INotifyCollectionChanged"/> and <see cref="INotifyPropertyChanged"/>.
/// </summary>
public class ObservableQueue<T> : IObservableQueue<T>, INotifyCollectionChanged, INotifyPropertyChanged
{
    private readonly Queue<T> _queue;

    /// <summary>
    /// Gets or sets the optional event dispatcher. When set, CollectionChanged and PropertyChanged are raised on the dispatcher's context (e.g. UI thread).
    /// </summary>
    public ICollectionEventDispatcher? EventDispatcher { get; set; }

    /// <summary>
    /// Gets or sets whether change notifications (CollectionChanged and PropertyChanged) are raised.
    /// When false, modifications to the queue do not raise any events. Default is true.
    /// </summary>
    public bool IsNotifyEnabled { get; set; } = true;

    /// <summary>
    /// Initializes a new empty ObservableQueue.
    /// </summary>
    public ObservableQueue()
    {
        _queue = new Queue<T>();
    }

    /// <summary>
    /// Initializes a new ObservableQueue with the specified Queue.
    /// </summary>
    /// <param name="queue">The Queue to initialize from.</param>
    public ObservableQueue(Queue<T> queue)
    {
        _queue = queue ?? new Queue<T>();
    }

    /// <summary>
    /// Initializes a new ObservableQueue with the specified collection.
    /// </summary>
    /// <param name="collection">The collection to initialize from.</param>
    public ObservableQueue(IEnumerable<T> collection)
    {
        _queue = collection != null ? new Queue<T>(collection) : new Queue<T>();
    }

    /// <summary>
    /// Initializes a new empty ObservableQueue and marshals change notifications to the specified synchronization context (e.g. UI thread).
    /// </summary>
    /// <param name="context">The context to raise CollectionChanged and PropertyChanged on; when null, notifications run on the current thread.</param>
    public ObservableQueue(SynchronizationContext? context) : this()
    {
        if (context != null) EventDispatcher = new SynchronizationContextCollectionEventDispatcher(context);
    }

    /// <summary>
    /// Initializes a new ObservableQueue with the specified queue and marshals change notifications to the specified synchronization context (e.g. UI thread).
    /// </summary>
    /// <param name="context">The context to raise CollectionChanged and PropertyChanged on; when null, notifications run on the current thread.</param>
    /// <param name="queue">The queue to initialize from.</param>
    public ObservableQueue(SynchronizationContext? context, Queue<T> queue) : this(queue)
    {
        if (context != null) EventDispatcher = new SynchronizationContextCollectionEventDispatcher(context);
    }

    /// <summary>
    /// Initializes a new ObservableQueue with the specified collection and marshals change notifications to the specified synchronization context (e.g. UI thread).
    /// </summary>
    /// <param name="context">The context to raise CollectionChanged and PropertyChanged on; when null, notifications run on the current thread.</param>
    /// <param name="collection">The collection to initialize from.</param>
    public ObservableQueue(SynchronizationContext? context, IEnumerable<T> collection) : this(collection)
    {
        if (context != null) EventDispatcher = new SynchronizationContextCollectionEventDispatcher(context);
    }

    /// <summary>
    /// Occurs when the queue changes.
    /// </summary>
    public event NotifyCollectionChangedEventHandler? CollectionChanged;

    /// <summary>
    /// Occurs when a property value changes.
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Gets the number of elements contained in the queue.
    /// </summary>
    public int Count => _queue.Count;

    /// <summary>
    /// Gets a value indicating whether the queue is read-only.
    /// </summary>
    public bool IsReadOnly => false;

    /// <summary>
    /// Copies the elements of the queue to an array, starting at a particular array index.
    /// </summary>
    /// <param name="array">The destination array.</param>
    /// <param name="arrayIndex">The zero-based index in array at which copying begins.</param>
    public void CopyTo(T[] array, int arrayIndex) => _queue.CopyTo(array, arrayIndex);

    /// <summary>
    /// Copies the elements of the queue to an array, starting at a particular array index.
    /// </summary>
    /// <param name="array">The destination array.</param>
    /// <param name="index">The zero-based index in array at which copying begins.</param>
    void ICollection.CopyTo(Array array, int index)
    {
        if (array is T[] tArray)
            _queue.CopyTo(tArray, index);
        else
        {
            foreach (var item in _queue)
                array.SetValue(item, index++);
        }
    }

    /// <summary>
    /// Gets an object that can be used to synchronize access to the queue.
    /// </summary>
    object ICollection.SyncRoot => ((ICollection)_queue).SyncRoot;

    /// <summary>
    /// Gets a value indicating whether access to the queue is synchronized (thread safe).
    /// </summary>
    bool ICollection.IsSynchronized => ((ICollection)_queue).IsSynchronized;

    /// <summary>
    /// Gets the number of elements contained in the queue.
    /// </summary>
    int ICollection.Count => _queue.Count;

    /// <summary>
    /// Adds an object to the end of the queue.
    /// </summary>
    /// <param name="item">The object to add to the queue.</param>
    public void Enqueue(T item)
    {
        _queue.Enqueue(item);
        OnPropertyChanged(nameof(Count));
        RaiseCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, _queue.Count - 1));
    }

    /// <summary>
    /// Removes and returns the object at the beginning of the queue.
    /// </summary>
    /// <returns>The object removed from the beginning of the queue.</returns>
    public T Dequeue()
    {
        T item = _queue.Dequeue();
        OnPropertyChanged(nameof(Count));
        RaiseCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item, 0));
        return item;
    }

    /// <summary>
    /// Returns the object at the beginning of the queue without removing it.
    /// </summary>
    /// <returns>The object at the beginning of the queue.</returns>
    public T Peek() => _queue.Peek();

    /// <summary>
    /// Removes all objects from the queue.
    /// </summary>
    public void Clear()
    {
        _queue.Clear();
        OnPropertyChanged(nameof(Count));
        RaiseCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }

    /// <summary>
    /// Resets the queue to the specified items: clears the queue, enqueues all items from the given enumeration,
    /// and raises a single <see cref="CollectionChanged"/> with <see cref="NotifyCollectionChangedAction.Reset"/>.
    /// </summary>
    /// <param name="items">The items to set. If null, the queue is cleared.</param>
    public void Reset(IEnumerable<T>? items)
    {
        _queue.Clear();
        if (items != null)
        {
            foreach (var x in items)
                _queue.Enqueue(x);
        }
        OnPropertyChanged(nameof(Count));
        RaiseCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }

    /// <summary>
    /// Returns an enumerator that iterates through the queue.
    /// </summary>
    /// <returns>An enumerator for the queue.</returns>
    public IEnumerator<T> GetEnumerator() => _queue.GetEnumerator();

    /// <summary>
    /// Returns an enumerator that iterates through the queue.
    /// </summary>
    /// <returns>An enumerator for the queue.</returns>
    IEnumerator IEnumerable.GetEnumerator() => _queue.GetEnumerator();

    /// <summary>
    /// Raises the <see cref="PropertyChanged"/> event.
    /// </summary>
    /// <param name="propertyName">The name of the property that changed.</param>
    private void OnPropertyChanged(string propertyName) =>
        RaisePropertyChanged(new PropertyChangedEventArgs(propertyName));

    /// <summary>
    /// Raises <see cref="CollectionChanged"/> on the dispatcher's context when <see cref="EventDispatcher"/> is set; otherwise raises on the current thread.
    /// </summary>
    private void RaiseCollectionChanged(NotifyCollectionChangedEventArgs e)
    {
        if (!IsNotifyEnabled || CollectionChanged == null) return;
        if (EventDispatcher != null && !EventDispatcher.IsCurrentContext)
        {
            EventDispatcher.Post(() => CollectionChanged?.Invoke(this, e));
            return;
        }
        CollectionChanged.Invoke(this, e);
    }

    /// <summary>
    /// Raises <see cref="PropertyChanged"/> on the dispatcher's context when <see cref="EventDispatcher"/> is set; otherwise raises on the current thread.
    /// </summary>
    private void RaisePropertyChanged(PropertyChangedEventArgs e)
    {
        if (!IsNotifyEnabled || PropertyChanged == null) return;
        if (EventDispatcher != null && !EventDispatcher.IsCurrentContext)
        {
            EventDispatcher.Post(() => PropertyChanged?.Invoke(this, e));
            return;
        }
        PropertyChanged.Invoke(this, e);
    }
}

/// <summary>
/// Defines an observable queue interface.
/// </summary>
public interface IObservableQueue<T> : IEnumerable<T>, IReadOnlyCollection<T>, ICollection, INotifyCollectionChanged, INotifyPropertyChanged;
