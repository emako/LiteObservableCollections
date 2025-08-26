using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;

namespace LiteObservableCollections;

public class ObservableQueue<T> : IObservableQueue<T>, INotifyCollectionChanged, INotifyPropertyChanged
{
    private readonly Queue<T> _queue = new();

    public event NotifyCollectionChangedEventHandler? CollectionChanged;

    public event PropertyChangedEventHandler? PropertyChanged;

    public int Count => _queue.Count;

    public bool IsReadOnly => false;

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

    object ICollection.SyncRoot => ((ICollection)_queue).SyncRoot;

    bool ICollection.IsSynchronized => ((ICollection)_queue).IsSynchronized;

    int ICollection.Count => _queue.Count;

    public void Enqueue(T item)
    {
        _queue.Enqueue(item);
        OnPropertyChanged(nameof(Count));
        CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, _queue.Count - 1));
    }

    public T Dequeue()
    {
        T item = _queue.Dequeue();
        OnPropertyChanged(nameof(Count));
        CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item, 0));
        return item;
    }

    public T Peek() => _queue.Peek();

    public void Clear()
    {
        _queue.Clear();
        OnPropertyChanged(nameof(Count));
        CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }

    public IEnumerator<T> GetEnumerator() => _queue.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => _queue.GetEnumerator();

    private void OnPropertyChanged(string propertyName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

public interface IObservableQueue<T> : IEnumerable<T>, IReadOnlyCollection<T>, ICollection, INotifyCollectionChanged, INotifyPropertyChanged;
