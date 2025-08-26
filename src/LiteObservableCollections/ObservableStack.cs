using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;

namespace LiteObservableCollections;

public class ObservableStack<T> : IObservableStack<T>, INotifyCollectionChanged, INotifyPropertyChanged
{
    private readonly Stack<T> _stack = new();

    public event NotifyCollectionChangedEventHandler? CollectionChanged;

    public event PropertyChangedEventHandler? PropertyChanged;

    public int Count => _stack.Count;

    public bool IsReadOnly => false;

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

    object ICollection.SyncRoot => ((ICollection)_stack).SyncRoot;

    bool ICollection.IsSynchronized => ((ICollection)_stack).IsSynchronized;

    int ICollection.Count => _stack.Count;

    public void Push(T item)
    {
        _stack.Push(item);
        OnPropertyChanged(nameof(Count));
        CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, _stack.Count - 1));
    }

    public T Pop()
    {
        T item = _stack.Pop();
        OnPropertyChanged(nameof(Count));
        CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item, 0));
        return item;
    }

    public T Peek() => _stack.Peek();

    public void Clear()
    {
        _stack.Clear();
        OnPropertyChanged(nameof(Count));
        CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }

    public IEnumerator<T> GetEnumerator() => _stack.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => _stack.GetEnumerator();

    private void OnPropertyChanged(string propertyName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

public interface IObservableStack<T> : IEnumerable<T>, IReadOnlyCollection<T>, ICollection, INotifyCollectionChanged, INotifyPropertyChanged;
