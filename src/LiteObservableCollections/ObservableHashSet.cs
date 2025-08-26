using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;

namespace LiteObservableCollections;

public class ObservableHashSet<T> : IObservableHashSet<T>, INotifyCollectionChanged, INotifyPropertyChanged
{
    private readonly HashSet<T> _set = [];

    public event NotifyCollectionChangedEventHandler? CollectionChanged;

    public event PropertyChangedEventHandler? PropertyChanged;

    public int Count => _set.Count;
    public bool IsReadOnly => false;

    public bool Add(T item)
    {
        bool added = _set.Add(item);
        if (added)
        {
            OnPropertyChanged(nameof(Count));
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item));
        }
        return added;
    }

    void ICollection<T>.Add(T item) => Add(item);

    public bool Remove(T item)
    {
        bool removed = _set.Remove(item);
        if (removed)
        {
            OnPropertyChanged(nameof(Count));
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item));
        }
        return removed;
    }

    public void Clear()
    {
        _set.Clear();
        OnPropertyChanged(nameof(Count));
        CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }

    public bool Contains(T item) => _set.Contains(item);

    public void CopyTo(T[] array, int arrayIndex) => _set.CopyTo(array, arrayIndex);

    public IEnumerator<T> GetEnumerator() => _set.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => _set.GetEnumerator();

    public void ExceptWith(IEnumerable<T> other) => _set.ExceptWith(other);

    public void IntersectWith(IEnumerable<T> other) => _set.IntersectWith(other);

    public bool IsProperSubsetOf(IEnumerable<T> other) => _set.IsProperSubsetOf(other);

    public bool IsProperSupersetOf(IEnumerable<T> other) => _set.IsProperSupersetOf(other);

    public bool IsSubsetOf(IEnumerable<T> other) => _set.IsSubsetOf(other);

    public bool IsSupersetOf(IEnumerable<T> other) => _set.IsSupersetOf(other);

    public bool Overlaps(IEnumerable<T> other) => _set.Overlaps(other);

    public bool SetEquals(IEnumerable<T> other) => _set.SetEquals(other);

    public void SymmetricExceptWith(IEnumerable<T> other) => _set.SymmetricExceptWith(other);

    public void UnionWith(IEnumerable<T> other) => _set.UnionWith(other);

    private void OnPropertyChanged(string propertyName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

public interface IObservableHashSet<T> : ISet<T>, INotifyCollectionChanged, INotifyPropertyChanged;
