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
    public void Listener_Reuses_BuiltIn_Subscriptions_Before_Dispatched_Collection_Event()
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

        Assert.Equal(0, raised);
        dispatcher.Flush();
        Assert.Equal(1, raised);
    }

    [Fact]
    public void Listener_Preserves_BuiltIn_Dispatch_Handler_Snapshot()
    {
        ObservableList<Person> source = new();
        RecordingEventDispatcher dispatcher = new(isCurrentContext: false);
        source.EventDispatcher = dispatcher;

        using CollectionItemPropertyChangedListener<Person> listener = new(source);
        int firstRaised = 0;
        int retainedRaised = 0;
        int lateRaised = 0;
        void First(object? _, CollectionItemPropertyChangedEventArgs<Person> e) => firstRaised++;
        void Retained(object? _, CollectionItemPropertyChangedEventArgs<Person> e) => retainedRaised++;
        void Late(object? _, CollectionItemPropertyChangedEventArgs<Person> e) => lateRaised++;

        listener.ItemPropertyChanged += First;
        listener.ItemPropertyChanged += Retained;

        Person person = new();
        source.Add(person);
        person.Name = "Ada";

        listener.ItemPropertyChanged -= First;
        listener.ItemPropertyChanged += Late;
        dispatcher.Flush();

        Assert.Equal(1, firstRaised);
        Assert.Equal(1, retainedRaised);
        Assert.Equal(0, lateRaised);
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
    public void Listener_Rejects_New_Handlers_After_Dispose()
    {
        ObservableList<Person> source = new();
        CollectionItemPropertyChangedListener<Person> listener = new(source);
        listener.Dispose();

        Assert.Throws<ObjectDisposedException>(() =>
        {
            listener.ItemPropertyChanged += (_, _) => { };
        });
    }
}
