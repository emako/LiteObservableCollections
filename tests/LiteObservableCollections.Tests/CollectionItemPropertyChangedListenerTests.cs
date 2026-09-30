using LiteObservableCollections.EventListeners;

namespace LiteObservableCollections.Tests;

public class CollectionItemPropertyChangedListenerTests
{
    [Fact]
    public void Listener_Raises_With_Listener_As_Sender()
    {
        ObservableList<Person> source = new();
        Person person = new();
        source.Add(person);

        using CollectionItemPropertyChangedListener<Person> listener = new(source);
        object? sender = null;
        listener.ItemPropertyChanged += (s, e) =>
        {
            sender = s;
            Assert.Same(person, e.Item);
        };

        person.Name = "Ada";

        Assert.Same(listener, sender);
    }

    [Fact]
    public void Listener_Tracks_Add_Remove_And_Dispose()
    {
        ObservableList<Person> source = new();
        using CollectionItemPropertyChangedListener<Person> listener = new(source);

        int raised = 0;
        listener.ItemPropertyChanged += (_, _) => raised++;

        Person person = new();
        source.Add(person);
        person.Name = "Ada";
        Assert.Equal(1, raised);

        source.Remove(person);
        person.Name = "Grace";
        Assert.Equal(1, raised);

        listener.Dispose();
        source.Add(person);
        person.Name = "Jean";
        Assert.Equal(1, raised);
    }

    [Fact]
    public void Listener_Requires_INotifyCollectionChanged()
    {
        List<Person> plain = [];
        Assert.Throws<ArgumentException>(() => new CollectionItemPropertyChangedListener<Person>(plain));
    }

    [Fact]
    public void Listener_Ignores_Null_Items_On_Typed_Nullable_Source()
    {
        ObservableList<Person> source = new();
        source.Add(null!);

        using CollectionItemPropertyChangedListener<Person> listener = new(source);
        int raised = 0;
        listener.ItemPropertyChanged += (_, _) => raised++;

        Person person = new();
        source.Add(person);
        person.Name = "Ada";

        Assert.Equal(1, raised);
    }

    [Fact]
    public void Listener_Implements_IItemPropertyObservable()
    {
        ObservableList<Person> source = new();
        using CollectionItemPropertyChangedListener<Person> listener = new(source);
        IItemPropertyObservable<Person> observable = listener;

        Person person = new();
        source.Add(person);

        int raised = 0;
        observable.ItemPropertyChanged += (_, _) => raised++;
        person.Name = "Ada";

        Assert.Equal(1, raised);
    }
}
