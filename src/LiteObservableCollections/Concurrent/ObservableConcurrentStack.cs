using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Specialized;
using System.ComponentModel;

namespace LiteObservableCollections.Concurrent;

/// <summary>
/// A thread-safe observable stack built on top of <see cref="ConcurrentStack{T}"/>.
/// This class provides common stack operations and raises <see cref="INotifyCollectionChanged"/>
/// and <see cref="INotifyPropertyChanged"/> events when the collection changes.
///
/// Notes:
/// - The underlying storage is <see cref="ConcurrentStack{T}"/>, which is safe for concurrent access.
/// - Events are raised after mutations; subscribers may observe further concurrent changes between the
///   mutation and the event handler execution.
/// </summary>
/// <typeparam name="T">Type of items in the stack.</typeparam>
public class ObservableConcurrentStack<T> : IEnumerable<T>, IReadOnlyCollection<T>, ICollection, INotifyCollectionChanged, INotifyPropertyChanged
{
    private readonly ConcurrentStack<T> _stack;

    /// <summary>
    /// Initializes an empty <see cref="ObservableConcurrentStack{T}"/>.
    /// </summary>
    public ObservableConcurrentStack()
    {
        _stack = new ConcurrentStack<T>();
    }

    /// <summary>
    /// Initializes a new instance that contains elements copied from the specified collection.
    /// </summary>
    /// <param name="collection">Collection whose elements are copied. If <c>null</c>, an empty stack is created.</param>
    public ObservableConcurrentStack(IEnumerable<T> collection)
    {
        _stack = collection != null ? new ConcurrentStack<T>(collection) : new ConcurrentStack<T>();
    }

    /// <summary>
    /// Occurs when the stack content changes (items pushed, popped or the collection reset).
    /// </summary>
    public event NotifyCollectionChangedEventHandler? CollectionChanged;

    /// <summary>
    /// Occurs when a property on the stack changes (for example, <see cref="Count"/>).
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Gets the number of elements contained in the stack.
    /// </summary>
    public int Count => _stack.Count;

    /// <summary>
    /// Gets a value indicating whether the collection is read-only. Always <c>false</c> here.
    /// </summary>
    public bool IsReadOnly => false;

    /// <summary>
    /// Copies the elements of the stack to an array, starting at a particular array index.
    /// </summary>
    public void CopyTo(T[] array, int arrayIndex) => _stack.CopyTo(array, arrayIndex);

    void ICollection.CopyTo(Array array, int index)
    {
        if (array is T[] tArray)
            _stack.CopyTo(tArray, index);
        else
        {
            foreach (var item in _stack)
                array.SetValue(item, index++);
        }
    }

    object ICollection.SyncRoot => ((ICollection)_stack).SyncRoot!;
    bool ICollection.IsSynchronized => ((ICollection)_stack).IsSynchronized;
    int ICollection.Count => _stack.Count;

    /// <summary>
    /// Inserts an object at the top of the stack and raises the appropriate notifications.
    /// </summary>
    public void Push(T item)
    {
        _stack.Push(item);
        OnPropertyChanged(nameof(Count));
        CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item));
    }

    /// <summary>
    /// Removes and returns the object at the top of the stack.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the stack is empty.</exception>
    public T Pop()
    {
        if (_stack.TryPop(out var item))
        {
            OnPropertyChanged(nameof(Count));
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item));
            return item;
        }
        throw new InvalidOperationException("Stack empty");
    }

    /// <summary>
    /// Returns the object at the top of the stack without removing it.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the stack is empty.</exception>
    public T Peek()
    {
        if (_stack.TryPeek(out var item)) return item;
        throw new InvalidOperationException("Stack empty");
    }

    /// <summary>
    /// Removes all objects from the stack.
    /// </summary>
    public void Clear()
    {
        while (_stack.TryPop(out _)) { }
        OnPropertyChanged(nameof(Count));
        CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }

    /// <summary>
    /// Returns an enumerator that iterates through the stack. Enumeration represents a snapshot-style view
    /// and may reflect concurrent updates.
    /// </summary>
    public IEnumerator<T> GetEnumerator() => _stack.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => _stack.GetEnumerator();

    /// <summary>
    /// Raises the <see cref="PropertyChanged"/> event for the specified property name.
    /// </summary>
    private void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
