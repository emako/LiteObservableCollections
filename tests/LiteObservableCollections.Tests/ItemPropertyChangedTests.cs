using System.Collections.Specialized;
using LiteObservableCollections.EventListeners;

namespace LiteObservableCollections.Tests;

public class ItemPropertyChangedTests
{
    [Fact]
    public void ItemPropertyChanged_Raises_When_Item_Property_Changes()
    {
        ObservableList<Person> list = new();
        Person person = new();
        list.Add(person);

        object? sender = null;
        string? propertyName = null;
        list.ItemPropertyChanged += (s, e) =>
        {
            sender = s;
            propertyName = e.PropertyChangedEventArgs.PropertyName;
            Assert.Same(person, e.Item);
        };

        person.Name = "Ada";

        Assert.Same(list, sender);
        Assert.Equal(nameof(Person.Name), propertyName);
    }

    [Fact]
    public void ItemPropertyChanged_Sender_Is_Collection_For_ObservableCollection()
    {
        ObservableCollection<Person> collection = new();
        Person person = new();
        collection.Add(person);

        object? sender = null;
        collection.ItemPropertyChanged += (s, _) => sender = s;

        person.Age = 30;

        Assert.Same(collection, sender);
    }

    [Fact]
    public void ItemPropertyChanged_Does_Not_Throw_When_Collection_Contains_Null()
    {
        ObservableList<Person?> list = new([null, new Person()]);
        int raised = 0;

        list.ItemPropertyChanged += (_, _) => raised++;
        list[1]!.Name = "Ada";

        Assert.Equal(1, raised);
    }

    [Fact]
    public void ItemPropertyChanged_Keeps_Subscriptions_When_IsNotifyEnabled_Is_False()
    {
        ObservableList<Person> list = new();
        Person existing = new();
        list.Add(existing);

        int raised = 0;
        list.ItemPropertyChanged += (_, _) => raised++;

        list.IsNotifyEnabled = false;
        Person added = new();
        list.Add(added);
        Person removed = existing;
        list.Remove(removed);
        list.IsNotifyEnabled = true;

        added.Name = "New";
        Assert.Equal(1, raised);

        removed.Name = "Old";
        Assert.Equal(1, raised);
    }

    [Fact]
    public void ItemPropertyChanged_Is_Suppressed_When_IsNotifyEnabled_Is_False()
    {
        ObservableList<Person> list = new();
        Person person = new();
        list.Add(person);

        int raised = 0;
        list.ItemPropertyChanged += (_, _) => raised++;

        list.IsNotifyEnabled = false;
        person.Name = "Ada";
        Assert.Equal(0, raised);

        list.IsNotifyEnabled = true;
        person.Name = "Grace";
        Assert.Equal(1, raised);
    }

    [Fact]
    public void ItemPropertyChanged_Uses_EventDispatcher_When_Not_Current_Context()
    {
        ObservableList<Person> list = new();
        Person person = new();
        list.Add(person);

        RecordingEventDispatcher dispatcher = new(isCurrentContext: false, runPostedImmediately: false);
        list.EventDispatcher = dispatcher;

        int raised = 0;
        list.ItemPropertyChanged += (_, _) => raised++;

        person.Name = "Ada";
        Assert.Equal(0, raised);
        Assert.Single(dispatcher.Posted);

        dispatcher.Flush();
        Assert.Equal(1, raised);
    }

    [Fact]
    public void ItemPropertyChanged_Unsubscribes_After_Remove()
    {
        ObservableList<Person> list = new();
        Person person = new();
        list.Add(person);

        int raised = 0;
        list.ItemPropertyChanged += (_, _) => raised++;

        list.Remove(person);
        person.Name = "Ada";

        Assert.Equal(0, raised);
    }

    [Fact]
    public void ItemPropertyChanged_Handles_Duplicate_References()
    {
        ObservableList<Person> list = new();
        Person person = new();
        list.Add(person);
        list.Add(person);

        int raised = 0;
        list.ItemPropertyChanged += (_, _) => raised++;

        person.Name = "Ada";
        Assert.Equal(1, raised);

        list.Remove(person);
        person.Name = "Grace";
        Assert.Equal(2, raised);

        list.Remove(person);
        person.Name = "Jean";
        Assert.Equal(2, raised);
    }

    [Fact]
    public void ItemPropertyChanged_Tracks_Replace_And_Reset()
    {
        ObservableList<Person> list = new();
        Person first = new();
        Person second = new();
        list.Add(first);

        int raised = 0;
        list.ItemPropertyChanged += (_, e) =>
        {
            raised++;
            Assert.Same(second, e.Item);
        };

        list[0] = second;
        first.Name = "Old";
        Assert.Equal(0, raised);

        second.Name = "New";
        Assert.Equal(1, raised);

        list.Reset([second]);
        second.Age = 1;
        Assert.Equal(2, raised);
    }

    [Fact]
    public void ItemPropertyChanged_Recreates_Subscriptions_After_Last_Handler_Removed()
    {
        ObservableList<Person> list = new();
        Person person = new();
        list.Add(person);

        int raised = 0;
        void Handler(object? _, CollectionItemPropertyChangedEventArgs<Person> e) => raised++;

        list.ItemPropertyChanged += Handler;
        person.Name = "Ada";
        Assert.Equal(1, raised);

        list.ItemPropertyChanged -= Handler;
        person.Name = "Grace";
        Assert.Equal(1, raised);

        Person later = new();
        list.Add(later);

        list.ItemPropertyChanged += Handler;
        later.Name = "Jean";
        person.Name = "Again";
        Assert.Equal(3, raised);
    }

    [Fact]
    public void ItemPropertyChanged_Throws_For_Value_Type_Items()
    {
        ObservableList<int> list = new();
        Assert.Throws<InvalidOperationException>(() =>
        {
            list.ItemPropertyChanged += (_, _) => { };
        });
    }

    [Fact]
    public void ItemPropertyChanged_Does_Not_Fire_For_Collection_Structural_Changes()
    {
        ObservableList<Person> list = new();
        int itemRaised = 0;
        List<NotifyCollectionChangedEventArgs> collectionEvents = CollectionChangeRecorder.Attach(list);

        list.ItemPropertyChanged += (_, _) => itemRaised++;
        list.Add(new Person());

        Assert.Equal(0, itemRaised);
        Assert.Single(collectionEvents);
        Assert.Equal(NotifyCollectionChangedAction.Add, collectionEvents[0].Action);
    }

    [Fact]
    public void IItemPropertyObservable_Exposes_Event_On_Abstraction()
    {
        ObservableList<Person> list = new();
        IItemPropertyObservable<Person> observable = list;
        Person person = new();
        list.Add(person);

        int raised = 0;
        observable.ItemPropertyChanged += (_, _) => raised++;
        person.Name = "Ada";

        Assert.Equal(1, raised);
    }
}
