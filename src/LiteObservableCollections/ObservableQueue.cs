using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;

namespace LiteObservableCollections;

/// <summary>
/// A lite observable queue that supports <see cref="INotifyCollectionChanged"/> and <see cref="INotifyPropertyChanged"/>.
/// </summary>
public class ObservableQueue<T> : IObservableQueue<T>, INotifyCollectionChanged, INotifyPropertyChanged
{
    private readonly Queue<T> _queue = new();

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
        CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, _queue.Count - 1));
    }

    /// <summary>
    /// Removes and returns the object at the beginning of the queue.
    /// </summary>
    /// <returns>The object removed from the beginning of the queue.</returns>
    public T Dequeue()
    {
        T item = _queue.Dequeue();
        OnPropertyChanged(nameof(Count));
        CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item, 0));
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
        CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
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
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

/// <summary>
/// Defines an observable queue interface.
/// </summary>
public interface IObservableQueue<T> : IEnumerable<T>, IReadOnlyCollection<T>, ICollection, INotifyCollectionChanged, INotifyPropertyChanged;
