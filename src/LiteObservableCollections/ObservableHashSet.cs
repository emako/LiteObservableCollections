using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;

namespace LiteObservableCollections;

/// <summary>
/// A lite observable hash set that supports <see cref="INotifyCollectionChanged"/> and <see cref="INotifyPropertyChanged"/>.
/// </summary>
public class ObservableHashSet<T> : IObservableHashSet<T>, INotifyCollectionChanged, INotifyPropertyChanged
{
    private readonly HashSet<T> _set = [];

    /// <summary>
    /// Occurs when the hash set changes.
    /// </summary>
    public event NotifyCollectionChangedEventHandler? CollectionChanged;

    /// <summary>
    /// Occurs when a property value changes.
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Gets the number of elements contained in the hash set.
    /// </summary>
    public int Count => _set.Count;
    
    /// <summary>
    /// Gets a value indicating whether the hash set is read-only.
    /// </summary>
    public bool IsReadOnly => false;

    /// <summary>
    /// Adds an element to the hash set.
    /// </summary>
    /// <param name="item">The element to add.</param>
    /// <returns>true if the element is added to the hash set; otherwise, false.</returns>
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

    /// <summary>
    /// Adds an element to the hash set via the ICollection interface.
    /// </summary>
    /// <param name="item">The element to add.</param>
    void ICollection<T>.Add(T item) => Add(item);

    /// <summary>
    /// Removes the specified element from the hash set.
    /// </summary>
    /// <param name="item">The element to remove.</param>
    /// <returns>true if the element is successfully found and removed; otherwise, false.</returns>
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

    /// <summary>
    /// Removes all elements from the hash set.
    /// </summary>
    public void Clear()
    {
        _set.Clear();
        OnPropertyChanged(nameof(Count));
        CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }

    /// <summary>
    /// Determines whether the hash set contains the specified element.
    /// </summary>
    /// <param name="item">The element to locate in the hash set.</param>
    /// <returns>true if the element is found; otherwise, false.</returns>
    public bool Contains(T item) => _set.Contains(item);

    /// <summary>
    /// Copies the elements of the hash set to an array, starting at a particular array index.
    /// </summary>
    /// <param name="array">The destination array.</param>
    /// <param name="arrayIndex">The zero-based index in array at which copying begins.</param>
    public void CopyTo(T[] array, int arrayIndex) => _set.CopyTo(array, arrayIndex);

    /// <summary>
    /// Returns an enumerator that iterates through the hash set.
    /// </summary>
    /// <returns>An enumerator for the hash set.</returns>
    public IEnumerator<T> GetEnumerator() => _set.GetEnumerator();

    /// <summary>
    /// Returns an enumerator that iterates through the hash set.
    /// </summary>
    /// <returns>An enumerator for the hash set.</returns>
    IEnumerator IEnumerable.GetEnumerator() => _set.GetEnumerator();

    /// <summary>
    /// Removes all elements in the specified collection from the hash set.
    /// </summary>
    /// <param name="other">The collection of items to remove from the hash set.</param>
    public void ExceptWith(IEnumerable<T> other) => _set.ExceptWith(other);

    /// <summary>
    /// Modifies the hash set to contain only elements that are also in a specified collection.
    /// </summary>
    /// <param name="other">The collection to compare to the current hash set.</param>
    public void IntersectWith(IEnumerable<T> other) => _set.IntersectWith(other);

    /// <summary>
    /// Determines whether the hash set is a proper subset of the specified collection.
    /// </summary>
    /// <param name="other">The collection to compare to the current hash set.</param>
    /// <returns>true if the hash set is a proper subset; otherwise, false.</returns>
    public bool IsProperSubsetOf(IEnumerable<T> other) => _set.IsProperSubsetOf(other);

    /// <summary>
    /// Determines whether the hash set is a proper superset of the specified collection.
    /// </summary>
    /// <param name="other">The collection to compare to the current hash set.</param>
    /// <returns>true if the hash set is a proper superset; otherwise, false.</returns>
    public bool IsProperSupersetOf(IEnumerable<T> other) => _set.IsProperSupersetOf(other);

    /// <summary>
    /// Determines whether the hash set is a subset of the specified collection.
    /// </summary>
    /// <param name="other">The collection to compare to the current hash set.</param>
    /// <returns>true if the hash set is a subset; otherwise, false.</returns>
    public bool IsSubsetOf(IEnumerable<T> other) => _set.IsSubsetOf(other);

    /// <summary>
    /// Determines whether the hash set is a superset of the specified collection.
    /// </summary>
    /// <param name="other">The collection to compare to the current hash set.</param>
    /// <returns>true if the hash set is a superset; otherwise, false.</returns>
    public bool IsSupersetOf(IEnumerable<T> other) => _set.IsSupersetOf(other);

    /// <summary>
    /// Determines whether the hash set and the specified collection share at least one common element.
    /// </summary>
    /// <param name="other">The collection to compare to the current hash set.</param>
    /// <returns>true if the hash set and other share at least one common element; otherwise, false.</returns>
    public bool Overlaps(IEnumerable<T> other) => _set.Overlaps(other);

    /// <summary>
    /// Determines whether the hash set and the specified collection contain the same elements.
    /// </summary>
    /// <param name="other">The collection to compare to the current hash set.</param>
    /// <returns>true if the hash set and other contain the same elements; otherwise, false.</returns>
    public bool SetEquals(IEnumerable<T> other) => _set.SetEquals(other);

    /// <summary>
    /// Modifies the hash set to contain only elements that are present either in the hash set or in the specified collection, but not both.
    /// </summary>
    /// <param name="other">The collection to compare to the current hash set.</param>
    public void SymmetricExceptWith(IEnumerable<T> other) => _set.SymmetricExceptWith(other);

    /// <summary>
    /// Modifies the hash set to contain all elements that are present in itself, the specified collection, or both.
    /// </summary>
    /// <param name="other">The collection to compare to the current hash set.</param>
    public void UnionWith(IEnumerable<T> other) => _set.UnionWith(other);

    /// <summary>
    /// Raises the <see cref="PropertyChanged"/> event.
    /// </summary>
    /// <param name="propertyName">The name of the property that changed.</param>
    private void OnPropertyChanged(string propertyName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

/// <summary>
/// Defines an observable hash set interface.
/// </summary>
public interface IObservableHashSet<T> : ISet<T>, INotifyCollectionChanged, INotifyPropertyChanged;
