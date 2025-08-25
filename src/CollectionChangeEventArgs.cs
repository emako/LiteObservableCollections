namespace LiteObservableCollections;

public sealed record CollectionChangeEventArgs<T>
{
    public IReadOnlyList<T> OldValues
    {
        get => _oldValues;
        set => _oldValues = value ?? throw new ArgumentNullException(nameof(value));
    }
    private IReadOnlyList<T> _oldValues = [];

    public IReadOnlyList<T> NewValues
    {
        get => _newValues;
        set => _newValues = value ?? throw new ArgumentNullException(nameof(value));
    }
    private IReadOnlyList<T> _newValues = [];
}
