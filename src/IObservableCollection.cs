namespace LiteObservableCollections;

public interface IObservableCollection<T>
{
    public event CollectionChangeEventHandler<T> CollectionChanged;
}
