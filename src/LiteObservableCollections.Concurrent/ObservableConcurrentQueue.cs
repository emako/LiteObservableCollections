using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Specialized;
using System.ComponentModel;

namespace LiteObservableCollections.Concurrent;

/// <summary>
/// A thread-safe observable queue built on top of <see cref="ConcurrentQueue{T}"/>.
/// This class provides common queue operations and raises <see cref="INotifyCollectionChanged"/>
/// and <see cref="INotifyPropertyChanged"/> events when the collection changes.
///
/// Notes:
/// - Underlying operations use <see cref="ConcurrentQueue{T}"/>, which is safe for concurrent access.
/// - Events are raised after the operation completes; subscribers may observe further concurrent
///   changes between the mutation and the handler execution.
/// </summary>
/// <typeparam name="T">Type of items in the queue.</typeparam>
public class ObservableConcurrentQueue<T> : IEnumerable<T>, IReadOnlyCollection<T>, ICollection, INotifyCollectionChanged, INotifyPropertyChanged
{
    private readonly ConcurrentQueue<T> _queue;

    /// <summary>
    /// Initializes an empty <see cref="ObservableConcurrentQueue{T}"/>.
    /// </summary>
    public ObservableConcurrentQueue()
    {
        _queue = new ConcurrentQueue<T>();
    }

    /// <summary>
    /// Initializes a new instance that contains elements copied from the specified collection.
    /// </summary>
    /// <param name="collection">Collection whose elements are copied. If <c>null</c>, an empty queue is created.</param>
    public ObservableConcurrentQueue(IEnumerable<T> collection)
    {
        _queue = collection != null ? new ConcurrentQueue<T>(collection) : new ConcurrentQueue<T>();
    }

    /// <summary>
    /// Occurs when the queue content changes (items enqueued, dequeued or the collection reset).
    /// </summary>
    public event NotifyCollectionChangedEventHandler? CollectionChanged;

    /// <summary>
    /// Occurs when a property on the queue changes (for example, <see cref="Count"/>).
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Gets the number of elements contained in the queue.
    /// </summary>
    public int Count => _queue.Count;

    /// <summary>
    /// Gets a value indicating whether the collection is read-only. Always <c>false</c> here.
    /// </summary>
    public bool IsReadOnly => false;

    /// <summary>
    /// Copies the elements of the queue to an array, starting at a particular array index.
    /// </summary>
    public void CopyTo(T[] array, int arrayIndex) => _queue.CopyTo(array, arrayIndex);

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

    object ICollection.SyncRoot => ((ICollection)_queue).SyncRoot!;
    bool ICollection.IsSynchronized => ((ICollection)_queue).IsSynchronized;
    int ICollection.Count => _queue.Count;

    /// <summary>
    /// Adds an object to the end of the queue and raises the appropriate notifications.
    /// </summary>
    public void Enqueue(T item)
    {
        _queue.Enqueue(item);
        OnPropertyChanged(nameof(Count));
        CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item));
    }

    /// <summary>
    /// Removes and returns the object at the beginning of the queue.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the queue is empty.</exception>
    public T Dequeue()
    {
        if (_queue.TryDequeue(out var item))
        {
            OnPropertyChanged(nameof(Count));
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item));
            return item;
        }
        throw new InvalidOperationException("Queue empty");
    }

    /// <summary>
    /// Returns the object at the beginning of the queue without removing it.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the queue is empty.</exception>
    public T Peek()
    {
        if (_queue.TryPeek(out var item)) return item;
        throw new InvalidOperationException("Queue empty");
    }

    /// <summary>
    /// Removes all objects from the queue.
    /// </summary>
    public void Clear()
    {
        while (_queue.TryDequeue(out _)) { }
        OnPropertyChanged(nameof(Count));
        CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }

    /// <summary>
    /// Returns an enumerator that iterates through the queue. Enumeration represents a snapshot-style view
    /// and may reflect concurrent updates.
    /// </summary>
    public IEnumerator<T> GetEnumerator() => _queue.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => _queue.GetEnumerator();

    /// <summary>
    /// Raises the <see cref="PropertyChanged"/> event for the specified property name.
    /// </summary>
    private void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
