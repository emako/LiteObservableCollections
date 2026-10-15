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
    public void Listener_Tracks_Arbitrary_NotifyCollectionChanged_Source()
    {
        System.Collections.ObjectModel.ObservableCollection<Person> source = new();
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
    }

    [Fact]
    public void Listener_Reuses_BuiltIn_Subscriptions_While_Notifications_Are_Suppressed()
    {
        ObservableList<Person> source = new();
        using CollectionItemPropertyChangedListener<Person> listener = new(source);

        int raised = 0;
        listener.ItemPropertyChanged += (_, _) => raised++;

        source.IsNotifyEnabled = false;
        Person person = new();
        source.Add(person);
        source.IsNotifyEnabled = true;

        person.Name = "Ada";
        Assert.Equal(1, raised);

        source.IsNotifyEnabled = false;
        source.Remove(person);
        source.IsNotifyEnabled = true;

        person.Name = "Grace";
        Assert.Equal(1, raised);
    }

    [Fact]
    public void Listener_Raises_While_Source_Notifications_Are_Suppressed()
    {
        Person person = new();
        ObservableCollection<Person> source = new([person]);
        using CollectionItemPropertyChangedListener<Person> listener = new(source);

        int raised = 0;
        listener.ItemPropertyChanged += (_, _) => raised++;

        source.IsNotifyEnabled = false;
        person.Name = "Ada";

        Assert.Equal(1, raised);
    }

    [Fact]
    public void Listener_Raises_Synchronously_Ignoring_Source_EventDispatcher()
    {
        ObservableList<Person> source = new();
        RecordingEventDispatcher dispatcher = new(isCurrentContext: false);
        source.EventDispatcher = dispatcher;

        using CollectionItemPropertyChangedListener<Person> listener = new(source);
        int raised = 0;
        listener.ItemPropertyChanged += (_, _) => raised++;

        Person person = new();
        source.Add(person);
        person.Name = "Ada";

        Assert.Equal(1, raised);
        Assert.Empty(dispatcher.Posted);
    }

    [Fact]
    public void Listener_Does_Not_Affect_Collection_ItemPropertyChanged_Gating()
    {
        Person person = new();
        ObservableList<Person> source = new([person]);
        using CollectionItemPropertyChangedListener<Person> listener = new(source);

        int listenerRaised = 0;
        int collectionRaised = 0;
        listener.ItemPropertyChanged += (_, _) => listenerRaised++;
        source.ItemPropertyChanged += (_, _) => collectionRaised++;

        source.IsNotifyEnabled = false;
        person.Name = "Ada";

        Assert.Equal(1, listenerRaised);
        Assert.Equal(0, collectionRaised);
    }

    [Fact]
    public void Listener_Handler_Exception_Does_Not_Suppress_Collection_ItemPropertyChanged()
    {
        Person person = new();
        ObservableList<Person> source = new([person]);
        int collectionRaised = 0;
        source.ItemPropertyChanged += (_, _) => collectionRaised++;
        using CollectionItemPropertyChangedListener<Person> listener = new(source);
        listener.ItemPropertyChanged += (_, _) => throw new InvalidOperationException("Listener failure.");

        Assert.Throws<InvalidOperationException>(() => person.Name = "Ada");

        Assert.Equal(1, collectionRaised);
    }

    [Fact]
    public void Listener_Removes_Single_Handler_From_Combined_Delegate()
    {
        Person person = new();
        ObservableList<Person> source = new([person]);
        using CollectionItemPropertyChangedListener<Person> listener = new(source);

        int firstRaised = 0;
        int secondRaised = 0;
        EventHandler<ItemPropertyChangedEventArgs<Person>> first = (_, _) => firstRaised++;
        EventHandler<ItemPropertyChangedEventArgs<Person>> second = (_, _) => secondRaised++;

        listener.ItemPropertyChanged += first + second;
        listener.ItemPropertyChanged -= first;
        person.Name = "Ada";

        Assert.Equal(0, firstRaised);
        Assert.Equal(1, secondRaised);
    }

    [Fact]
    public void Listener_Dispose_Releases_BuiltIn_Item_Subscriptions()
    {
        CountingNotifyItem item = new();
        ObservableList<CountingNotifyItem> source = new([item]);
        CollectionItemPropertyChangedListener<CountingNotifyItem> listener = new(source);

        Assert.Equal(1, item.AddedHandlers);
        listener.Dispose();

        Assert.Equal(1, item.RemovedHandlers);
    }

    [Fact]
    public void Listener_Observes_Runtime_Types_Of_Unconstrained_Items()
    {
        Person listPerson = new();
        Person bclPerson = new();
        ObservableList<object?> list = new([1, null, listPerson]);
        System.Collections.ObjectModel.ObservableCollection<object?> bcl = new([2, null, bclPerson]);
        using CollectionItemPropertyChangedListener<object?> listListener = new(list);
        using CollectionItemPropertyChangedListener<object?> bclListener = new(bcl);

        List<object?> changed = [];
        listListener.ItemPropertyChanged += (_, e) => changed.Add(e.Item);
        bclListener.ItemPropertyChanged += (_, e) => changed.Add(e.Item);

        listPerson.Name = "Ada";
        bclPerson.Name = "Grace";

        Assert.Equal(new object?[] { listPerson, bclPerson }, changed);
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

    [Fact]
    public void Listener_Ignores_Item_Changes_After_Dispose_Even_With_New_Handlers()
    {
        Person person = new();
        ObservableList<Person> source = new([person]);
        CollectionItemPropertyChangedListener<Person> listener = new(source);
        listener.Dispose();

        int raised = 0;
        Exception? exception = Record.Exception(() => listener.ItemPropertyChanged += (_, _) => raised++);
        person.Name = "Ada";

        Assert.Null(exception);
        Assert.Equal(0, raised);
    }
}
