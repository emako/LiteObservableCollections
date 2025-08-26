using System.Collections.Specialized;
using System.ComponentModel;
using System.Collections;

namespace LiteObservableCollections;

/// <summary>
/// A lite observable list that supports <see cref="INotifyCollectionChanged"/> and <see cref="INotifyPropertyChanged"/>.
/// </summary>
/// <summary>
/// Represents a list that notifies listeners of dynamic changes, such as when items get added, removed, or the whole list is refreshed.
/// </summary>
public partial class ObservableList<T> : IObservableList<T>, INotifyCollectionChanged, INotifyPropertyChanged
{
    private readonly List<T> _items;

    /// <summary>
    /// Initializes a new empty ObservableList.
    /// </summary>
    public ObservableList()
    {
        _items = [];
    }

    /// <summary>
    /// Initializes a new ObservableList with the specified List.
    /// </summary>
    /// <param name="list">The List to initialize from.</param>
    public ObservableList(List<T> list)
    {
        _items = list ?? [];
    }

    /// <summary>
    /// Initializes a new ObservableList with the specified collection.
    /// </summary>
    /// <param name="collection">The collection to initialize from.</param>
    public ObservableList(IEnumerable<T> collection)
    {
        _items = collection != null ? [.. collection] : [];
    }

    /// <summary>
    /// Occurs when the collection changes.
    /// </summary>
    public event NotifyCollectionChangedEventHandler? CollectionChanged;

    /// <summary>
    /// Occurs when a property value changes.
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Gets or sets the element at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index of the element to get or set.</param>
    /// <returns>The element at the specified index.</returns>
    public T this[int index]
    {
        get => _items[index];
        set
        {
            T oldItem = _items[index];
            _items[index] = value;
            OnPropertyChanged("Item[]"); // Notify that the this[] has changed
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, value, oldItem, index));
        }
    }

    /// <summary>
    /// Gets the number of elements contained in the list.
    /// </summary>
    public int Count => _items.Count;

    /// <summary>
    /// Gets a value indicating whether the list is read-only.
    /// </summary>
    public bool IsReadOnly => false;

    /// <summary>
    /// Adds an item to the list.
    /// </summary>
    /// <param name="item">The object to add to the list.</param>
    public void Add(T item)
    {
        _items.Add(item);
        OnPropertyChanged(nameof(Count));
        OnPropertyChanged("Item[]"); // Notify that the this[] has changed
        CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, _items.Count - 1));
    }

    /// <summary>
    /// Adds the elements of the specified collection to the end of the list.
    /// </summary>
    /// <param name="items">The collection whose elements should be added to the end of the list.</param>
    public void AddRange(IEnumerable<T> items)
    {
        if (items == null) return;
        foreach (T item in items)
            Add(item);

        OnPropertyChanged(nameof(Count));
        OnPropertyChanged("Item[]"); // Notify that the this[] has changed
        CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }

    /// <summary>
    /// Removes the first occurrence of a specific object from the list.
    /// </summary>
    /// <param name="item">The object to remove from the list.</param>
    /// <returns>true if item was successfully removed; otherwise, false.</returns>
    public bool Remove(T item)
    {
        int index = _items.IndexOf(item);
        if (index < 0) return false;

        _items.RemoveAt(index);
        OnPropertyChanged(nameof(Count));
        OnPropertyChanged("Item[]"); // Notify that the this[] has changed
        CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item, index));
        return true;
    }

    /// <summary>
    /// Removes all items from the list.
    /// </summary>
    public void Clear()
    {
        _items.Clear();
        OnPropertyChanged(nameof(Count));
        OnPropertyChanged("Item[]"); // Notify that the this[] has changed
        CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }

    /// <summary>
    /// Determines whether the list contains a specific value.
    /// </summary>
    /// <param name="item">The object to locate in the list.</param>
    /// <returns>true if item is found; otherwise, false.</returns>
    public bool Contains(T item) => _items.Contains(item);

    /// <summary>
    /// Copies the elements of the list to an array, starting at a particular array index.
    /// </summary>
    /// <param name="array">The destination array.</param>
    /// <param name="arrayIndex">The zero-based index in array at which copying begins.</param>
    public void CopyTo(T[] array, int arrayIndex) => _items.CopyTo(array, arrayIndex);

    /// <summary>
    /// Returns an enumerator that iterates through the list.
    /// </summary>
    /// <returns>An enumerator for the list.</returns>
    public IEnumerator<T> GetEnumerator() => _items.GetEnumerator();

    /// <summary>
    /// Returns an enumerator that iterates through the list.
    /// </summary>
    /// <returns>An enumerator for the list.</returns>
    IEnumerator IEnumerable.GetEnumerator() => _items.GetEnumerator();

    /// <summary>
    /// Determines the index of a specific item in the list.
    /// </summary>
    /// <param name="item">The object to locate in the list.</param>
    /// <returns>The index of item if found; otherwise, -1.</returns>
    public int IndexOf(T item) => _items.IndexOf(item);

    /// <summary>
    /// Inserts an item to the list at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index at which item should be inserted.</param>
    /// <param name="item">The object to insert into the list.</param>
    public void Insert(int index, T item)
    {
        _items.Insert(index, item);
        OnPropertyChanged(nameof(Count));
        OnPropertyChanged("Item[]");
        CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, index));
    }

    /// <summary>
    /// Removes the item at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index of the item to remove.</param>
    public void RemoveAt(int index)
    {
        T oldItem = _items[index];
        _items.RemoveAt(index);
        OnPropertyChanged(nameof(Count));
        OnPropertyChanged("Item[]");
        CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, oldItem, index));
    }

    /// <summary>
    /// Raises the <see cref="PropertyChanged"/> event.
    /// </summary>
    /// <param name="propertyName">The name of the property that changed.</param>
    private void OnPropertyChanged(string propertyName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

/// <summary>
/// Defines an observable list interface that supports range addition.
/// </summary>
public interface IObservableList<T> : IList<T>, INotifyCollectionChanged, INotifyPropertyChanged
{
    /// <summary>
    /// Adds the elements of the specified collection to the end of the list.
    /// </summary>
    /// <param name="items">The collection whose elements should be added to the end of the list.</param>
    public void AddRange(IEnumerable<T> items);
}
