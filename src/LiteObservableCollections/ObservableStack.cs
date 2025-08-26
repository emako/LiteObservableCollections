using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;

namespace LiteObservableCollections;

/// <summary>
/// A lite observable stack that supports <see cref="INotifyCollectionChanged"/> and <see cref="INotifyPropertyChanged"/>.
/// </summary>
public class ObservableStack<T> : IObservableStack<T>, INotifyCollectionChanged, INotifyPropertyChanged
{
    private readonly Stack<T> _stack;

    /// <summary>
    /// Initializes a new empty ObservableStack.
    /// </summary>
    public ObservableStack()
    {
        _stack = new Stack<T>();
    }

    /// <summary>
    /// Initializes a new ObservableStack with the specified Stack.
    /// </summary>
    /// <param name="stack">The Stack to initialize from.</param>
    public ObservableStack(Stack<T> stack)
    {
        _stack = stack ?? new Stack<T>();
    }

    /// <summary>
    /// Initializes a new ObservableStack with the specified collection.
    /// </summary>
    /// <param name="collection">The collection to initialize from.</param>
    public ObservableStack(IEnumerable<T> collection)
    {
        _stack = collection != null ? new Stack<T>(collection) : new Stack<T>();
    }

    /// <summary>
    /// Occurs when the stack changes.
    /// </summary>
    public event NotifyCollectionChangedEventHandler? CollectionChanged;

    /// <summary>
    /// Occurs when a property value changes.
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Gets the number of elements contained in the stack.
    /// </summary>
    public int Count => _stack.Count;

    /// <summary>
    /// Gets a value indicating whether the stack is read-only.
    /// </summary>
    public bool IsReadOnly => false;

    /// <summary>
    /// Copies the elements of the stack to an array, starting at a particular array index.
    /// </summary>
    /// <param name="array">The destination array.</param>
    /// <param name="arrayIndex">The zero-based index in array at which copying begins.</param>
    public void CopyTo(T[] array, int arrayIndex) => _stack.CopyTo(array, arrayIndex);

    /// <summary>
    /// Copies the elements of the stack to an array, starting at a particular array index.
    /// </summary>
    /// <param name="array">The destination array.</param>
    /// <param name="index">The zero-based index in array at which copying begins.</param>
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

    /// <summary>
    /// Gets an object that can be used to synchronize access to the stack.
    /// </summary>
    object ICollection.SyncRoot => ((ICollection)_stack).SyncRoot;

    /// <summary>
    /// Gets a value indicating whether access to the stack is synchronized (thread safe).
    /// </summary>
    bool ICollection.IsSynchronized => ((ICollection)_stack).IsSynchronized;

    /// <summary>
    /// Gets the number of elements contained in the stack.
    /// </summary>
    int ICollection.Count => _stack.Count;

    /// <summary>
    /// Inserts an object at the top of the stack.
    /// </summary>
    /// <param name="item">The object to push onto the stack.</param>
    public void Push(T item)
    {
        _stack.Push(item);
        OnPropertyChanged(nameof(Count));
        CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, _stack.Count - 1));
    }

    /// <summary>
    /// Removes and returns the object at the top of the stack.
    /// </summary>
    /// <returns>The object removed from the top of the stack.</returns>
    public T Pop()
    {
        T item = _stack.Pop();
        OnPropertyChanged(nameof(Count));
        CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item, 0));
        return item;
    }

    /// <summary>
    /// Returns the object at the top of the stack without removing it.
    /// </summary>
    /// <returns>The object at the top of the stack.</returns>
    public T Peek() => _stack.Peek();

    /// <summary>
    /// Removes all objects from the stack.
    /// </summary>
    public void Clear()
    {
        _stack.Clear();
        OnPropertyChanged(nameof(Count));
        CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }

    /// <summary>
    /// Returns an enumerator that iterates through the stack.
    /// </summary>
    /// <returns>An enumerator for the stack.</returns>
    public IEnumerator<T> GetEnumerator() => _stack.GetEnumerator();

    /// <summary>
    /// Returns an enumerator that iterates through the stack.
    /// </summary>
    /// <returns>An enumerator for the stack.</returns>
    IEnumerator IEnumerable.GetEnumerator() => _stack.GetEnumerator();

    /// <summary>
    /// Raises the <see cref="PropertyChanged"/> event.
    /// </summary>
    /// <param name="propertyName">The name of the property that changed.</param>
    private void OnPropertyChanged(string propertyName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

/// <summary>
/// Defines an observable stack interface.
/// </summary>
public interface IObservableStack<T> : IEnumerable<T>, IReadOnlyCollection<T>, ICollection, INotifyCollectionChanged, INotifyPropertyChanged;
