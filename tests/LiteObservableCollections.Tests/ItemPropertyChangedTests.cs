using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;

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
    public void ItemPropertyChanged_Drops_Queued_Event_When_Item_Is_Removed()
    {
        ObservableList<Person> list = new();
        Person person = new();
        list.Add(person);

        RecordingEventDispatcher dispatcher = new(isCurrentContext: false);
        list.EventDispatcher = dispatcher;

        int raised = 0;
        list.ItemPropertyChanged += (_, _) => raised++;

        person.Name = "Ada";
        list.Remove(person);
        dispatcher.Flush();

        Assert.Equal(0, raised);
    }

    [Fact]
    public void ItemPropertyChanged_Keeps_Queued_Event_When_Replaced_With_Same_Instance()
    {
        Person person = new();
        ObservableList<Person> list = new([person]);

        RecordingEventDispatcher dispatcher = new(isCurrentContext: false);
        list.EventDispatcher = dispatcher;

        int raised = 0;
        list.ItemPropertyChanged += (_, _) => raised++;

        person.Name = "Ada";
        list[0] = person;
        dispatcher.Flush();

        Assert.Equal(1, raised);
    }

    [Fact]
    public void ItemPropertyChanged_Checks_IsNotifyEnabled_When_Item_Changes_Like_CollectionChanged()
    {
        ObservableList<Person> list = new();
        Person person = new();
        list.Add(person);

        RecordingEventDispatcher dispatcher = new(isCurrentContext: false);
        list.EventDispatcher = dispatcher;

        int itemRaised = 0;
        int collectionRaised = 0;
        list.ItemPropertyChanged += (_, _) => itemRaised++;
        list.CollectionChanged += (_, _) => collectionRaised++;

        person.Name = "Ada";
        list.Add(new Person());
        list.IsNotifyEnabled = false;
        dispatcher.Flush();
        Assert.Equal(1, itemRaised);
        Assert.Equal(1, collectionRaised);

        person.Name = "Grace";
        Assert.Empty(dispatcher.Posted);
    }

    [Fact]
    public void ItemPropertyChanged_Queued_Event_Uses_Handlers_Attached_At_Delivery()
    {
        ObservableList<Person> list = new();
        Person person = new();
        list.Add(person);

        RecordingEventDispatcher dispatcher = new(isCurrentContext: false);
        list.EventDispatcher = dispatcher;

        int firstRaised = 0;
        int retainedRaised = 0;
        int lateRaised = 0;
        void First(object? _, ItemPropertyChangedEventArgs<Person> e) => firstRaised++;
        void Retained(object? _, ItemPropertyChangedEventArgs<Person> e) => retainedRaised++;
        void Late(object? _, ItemPropertyChangedEventArgs<Person> e) => lateRaised++;

        list.ItemPropertyChanged += First;
        list.ItemPropertyChanged += Retained;
        person.Name = "Ada";

        list.ItemPropertyChanged -= First;
        list.ItemPropertyChanged += Late;
        dispatcher.Flush();

        Assert.Equal(0, firstRaised);
        Assert.Equal(1, retainedRaised);
        Assert.Equal(1, lateRaised);
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
        void Handler(object? _, ItemPropertyChangedEventArgs<Person> e) => raised++;

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
    public void Remove_Uses_The_Stored_Instance_For_Equal_Items()
    {
        EqualPerson first = new(1);
        EqualPerson second = new(1);
        ObservableList<EqualPerson> list = new([first, second]);

        object? removedItem = null;
        list.CollectionChanged += (_, e) => removedItem = e.OldItems?[0];

        int raised = 0;
        list.ItemPropertyChanged += (_, _) => raised++;

        Assert.True(list.Remove(second));
        Assert.Same(first, removedItem);
        Assert.Same(second, Assert.Single(list));

        first.Name = "removed";
        Assert.Equal(0, raised);

        second.Name = "retained";
        Assert.Equal(1, raised);
    }

    [Fact]
    public void ItemPropertyChanged_Allows_Value_Type_Items_And_Ignores_Them()
    {
        ObservableList<int> list = new();
        int raised = 0;

        Exception? exception = Record.Exception(() => list.ItemPropertyChanged += (_, _) => raised++);
        list.Add(1);

        Assert.Null(exception);
        Assert.Equal(0, raised);
    }

    [Fact]
    public void ItemPropertyChanged_Observes_Runtime_Type_Through_Object_Collection()
    {
        ObservableList<object> list = new();
        Person person = new();

        int raised = 0;
        list.ItemPropertyChanged += (_, e) =>
        {
            raised++;
            Assert.Same(person, e.Item);
        };

        list.Add(person);
        person.Name = "Ada";

        Assert.Equal(1, raised);
    }

    [Fact]
    public void ItemPropertyChanged_Does_Not_Require_Item_As_PropertyChanged_Sender()
    {
        NonStandardNotifyItem item = new();
        ObservableList<NonStandardNotifyItem> list = new([item]);

        int raised = 0;
        list.ItemPropertyChanged += (_, e) =>
        {
            raised++;
            Assert.Same(item, e.Item);
        };

        item.RaiseWithNullSender("Value");

        Assert.Equal(1, raised);
    }

    [Fact]
    public void Reset_With_Same_Items_Does_Not_Reattach_PropertyChanged_Handlers()
    {
        CountingNotifyItem item = new();
        ObservableList<CountingNotifyItem> list = new([item]);

        int raised = 0;
        void Handler(object? _, ItemPropertyChangedEventArgs<CountingNotifyItem> e) => raised++;

        list.ItemPropertyChanged += Handler;
        Assert.Equal(1, item.AddedHandlers);
        Assert.Equal(0, item.RemovedHandlers);

        list.Reverse();
        item.Raise("Value");

        Assert.Equal(1, item.AddedHandlers);
        Assert.Equal(0, item.RemovedHandlers);
        Assert.Equal(1, raised);

        list.ItemPropertyChanged -= Handler;
        Assert.Equal(1, item.RemovedHandlers);
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
    public void Collection_Interfaces_Expose_ItemPropertyChanged()
    {
        Person person = new();
        ObservableList<Person> list = new([person]);
        ObservableCollection<Person> collection = new([person]);
        IObservableList<Person> listObservable = list;
        IObservableCollection<Person> collectionObservable = collection;

        string? listProperty = null;
        string? collectionProperty = null;
        listObservable.ItemPropertyChanged += (_, e) => listProperty = e.PropertyName;
        collectionObservable.ItemPropertyChanged += (_, e) => collectionProperty = e.PropertyName;
        person.Name = "Ada";

        Assert.Equal(nameof(Person.Name), listProperty);
        Assert.Equal(nameof(Person.Name), collectionProperty);
    }

    [Fact]
    public void Reset_Counts_Duplicate_New_Items()
    {
        ObservableList<Person> list = new();
        int raised = 0;
        list.ItemPropertyChanged += (_, _) => raised++;

        Person person = new();
        list.Reset([person, person]);
        list.Remove(person);

        person.Name = "Ada";
        Assert.Equal(1, raised);

        list.Remove(person);
        person.Name = "Grace";
        Assert.Equal(1, raised);
    }

    [Fact]
    public void Clear_Unsubscribes_All_Items()
    {
        CountingNotifyItem first = new();
        CountingNotifyItem second = new();
        ObservableList<CountingNotifyItem> list = new([first, second, first]);

        int raised = 0;
        list.ItemPropertyChanged += (_, _) => raised++;
        list.Clear();

        first.Raise("Value");
        second.Raise("Value");

        Assert.Equal(0, raised);
        Assert.Equal(1, first.RemovedHandlers);
        Assert.Equal(1, second.RemovedHandlers);
    }

    [Fact]
    public void Sort_And_Move_Keep_Subscriptions_Without_Reattaching()
    {
        CountingNotifyItem first = new();
        CountingNotifyItem second = new();
        ObservableList<CountingNotifyItem> list = new([first, second]);

        int raised = 0;
        list.ItemPropertyChanged += (_, _) => raised++;

        list.Sort((x, y) => ReferenceEquals(x, y) ? 0 : ReferenceEquals(x, second) ? -1 : 1);
        list.Move(0, 1);
        first.Raise("Value");
        second.Raise("Value");

        Assert.Equal(2, raised);
        Assert.Equal(1, first.AddedHandlers);
        Assert.Equal(0, first.RemovedHandlers);
    }

    [Fact]
    public void RemoveRange_Unsubscribes_The_Stored_Instance_For_Equal_Items()
    {
        EqualPerson first = new(1);
        EqualPerson second = new(1);
        ObservableList<EqualPerson> list = new([first, second]);

        int raised = 0;
        list.ItemPropertyChanged += (_, _) => raised++;

        list.RemoveRange([second]);

        first.Name = "removed";
        Assert.Equal(0, raised);

        second.Name = "retained";
        Assert.Equal(1, raised);
    }

    [Fact]
    public void AddRange_Subscribes_Only_New_Items()
    {
        CountingNotifyItem existing = new();
        CountingNotifyItem added = new();
        ObservableList<CountingNotifyItem> list = new([existing]);

        int raised = 0;
        list.ItemPropertyChanged += (_, _) => raised++;
        list.AddRange([added]);

        added.Raise("Value");
        existing.Raise("Value");

        Assert.Equal(2, raised);
        Assert.Equal(1, existing.AddedHandlers);
        Assert.Equal(1, added.AddedHandlers);
    }

    [Fact]
    public void Failed_Item_Subscription_Does_Not_Leave_Partial_Subscriptions()
    {
        CountingNotifyItem good = new();
        ObservableList<INotifyPropertyChanged> list = new([good, new ThrowingNotifyItem()]);

        int raised = 0;
        Assert.Throws<InvalidOperationException>(() => list.ItemPropertyChanged += (_, _) => raised++);

        good.Raise("Value");
        Assert.Equal(0, raised);
        Assert.Equal(1, good.RemovedHandlers);

        list.RemoveAt(1);
        list.ItemPropertyChanged += (_, _) => raised++;
        good.Raise("Value");
        Assert.Equal(1, raised);
    }

    [Fact]
    public void AddRange_Does_Not_Reattach_Existing_Items()
    {
        CountingNotifyItem[] existing = [.. Enumerable.Range(0, 100).Select(_ => new CountingNotifyItem())];
        ObservableList<CountingNotifyItem> list = new(existing);
        list.ItemPropertyChanged += (_, _) => { };

        for (int i = 0; i < 100; i++)
            list.AddRange([new CountingNotifyItem()]);

        Assert.All(existing, item =>
        {
            Assert.Equal(1, item.AddedHandlers);
            Assert.Equal(0, item.RemovedHandlers);
        });
    }

    [Fact]
    public void AddRange_Cost_Does_Not_Grow_With_Existing_Item_Count()
    {
        TimeSpan small = MeasureAddRange(existingCount: 100);
        TimeSpan large = MeasureAddRange(existingCount: 100_000);

        double limit = Math.Max(200, small.TotalMilliseconds * 20);
        Assert.True(large.TotalMilliseconds < limit, $"AddRange took {large.TotalMilliseconds} ms with 100k items vs {small.TotalMilliseconds} ms with 100.");

        static TimeSpan MeasureAddRange(int existingCount)
        {
            ObservableList<Person> list = [.. Enumerable.Range(0, existingCount).Select(_ => new Person())];
            list.ItemPropertyChanged += (_, _) => { };

            Stopwatch stopwatch = Stopwatch.StartNew();
            for (int i = 0; i < 1_000; i++)
                list.AddRange([new Person()]);
            return stopwatch.Elapsed;
        }
    }

    [Fact]
    public void Items_Are_Subscribed_Only_While_Handlers_Are_Attached()
    {
        CountingNotifyItem item = new();
        ObservableList<CountingNotifyItem> list = new([item]);
        Assert.Equal(0, item.AddedHandlers);

        int raised = 0;
        void Handler(object? _, ItemPropertyChangedEventArgs<CountingNotifyItem> e) => raised++;

        list.ItemPropertyChanged += Handler;
        Assert.Equal(1, item.AddedHandlers);
        item.Raise("Value");
        Assert.Equal(1, raised);

        list.ItemPropertyChanged -= Handler;
        Assert.Equal(1, item.RemovedHandlers);
        item.Raise("Value");
        Assert.Equal(1, raised);
    }

    [Fact]
    public void Interface_Consumers_Receive_ItemPropertyChanged_Without_Extra_Configuration()
    {
        Person person = new();
        IObservableList<Person> list = new ObservableList<Person>([person]);
        IObservableCollection<Person> collection = new ObservableCollection<Person>([person]);

        int listRaised = 0;
        int collectionRaised = 0;
        list.ItemPropertyChanged += (_, _) => listRaised++;
        collection.ItemPropertyChanged += (_, _) => collectionRaised++;
        person.Name = "Ada";

        Assert.Equal(1, listRaised);
        Assert.Equal(1, collectionRaised);
    }

    [Fact]
    public void ItemPropertyChanged_Drops_Queued_Event_When_Item_Is_Removed_And_Added_Again()
    {
        Person person = new();
        ObservableList<Person> list = new([person]);
        RecordingEventDispatcher dispatcher = new(isCurrentContext: false);
        list.EventDispatcher = dispatcher;

        int raised = 0;
        list.ItemPropertyChanged += (_, _) => raised++;

        person.Name = "Ada";
        list.Remove(person);
        list.Add(person);
        dispatcher.Flush();
        Assert.Equal(0, raised);

        person.Name = "Grace";
        dispatcher.Flush();
        Assert.Equal(1, raised);
    }

    [Fact]
    public void Mutation_Is_Notified_When_Item_Subscription_Throws()
    {
        ObservableList<INotifyPropertyChanged> list = new();
        list.ItemPropertyChanged += (_, _) => { };
        List<NotifyCollectionChangedEventArgs> collectionEvents = CollectionChangeRecorder.Attach(list);
        List<string?> propertyNames = [];
        list.PropertyChanged += (_, e) => propertyNames.Add(e.PropertyName);

        Assert.Throws<InvalidOperationException>(() => list.Add(new ThrowingNotifyItem()));

        Assert.Single(list);
        Assert.Equal(NotifyCollectionChangedAction.Add, Assert.Single(collectionEvents).Action);
        Assert.Equal(["Count", "Item[]"], propertyNames);
    }

    [Fact]
    public void Replace_Unsubscribes_Old_Item_When_New_Item_Subscription_Throws()
    {
        CountingNotifyItem oldItem = new();
        ObservableCollection<INotifyPropertyChanged> collection = new([oldItem]);
        int raised = 0;
        collection.ItemPropertyChanged += (_, _) => raised++;
        List<NotifyCollectionChangedEventArgs> collectionEvents = CollectionChangeRecorder.Attach(collection);

        Assert.Throws<InvalidOperationException>(() => collection[0] = new ThrowingNotifyItem());

        Assert.Equal(1, oldItem.RemovedHandlers);
        oldItem.Raise("Value");
        Assert.Equal(0, raised);
        Assert.Equal(NotifyCollectionChangedAction.Replace, Assert.Single(collectionEvents).Action);
    }

#if NET
    [Fact]
    public void Mutations_Do_Not_Allocate_Without_Subscribers()
    {
        Person[] people = [.. Enumerable.Range(0, 100).Select(_ => new Person())];
        ObservableList<Person> list = new(new List<Person>(people.Length));
        ObservableCollection<Person> collection = new();
        AddAndRemoveAll(list, people);
        AddAndRemoveAll(collection, people);

        long before = GC.GetAllocatedBytesForCurrentThread();
        AddAndRemoveAll(list, people);
        AddAndRemoveAll(collection, people);
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.Equal(0, allocated);

        static void AddAndRemoveAll(IList<Person> target, Person[] items)
        {
            foreach (Person item in items)
                target.Add(item);
            for (int i = target.Count - 1; i >= 0; i--)
                target.RemoveAt(i);
        }
    }

    [Fact]
    public void Value_Type_Items_Are_Not_Boxed_While_Handlers_Are_Attached()
    {
        ObservableList<int> list = new(new List<int>(200));
        list.ItemPropertyChanged += (_, _) => { };
        AddAndRemoveAll(list);

        long before = GC.GetAllocatedBytesForCurrentThread();
        AddAndRemoveAll(list);
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.Equal(0, allocated);

        static void AddAndRemoveAll(ObservableList<int> target)
        {
            for (int i = 0; i < 100; i++)
                target.Add(i);
            for (int i = target.Count - 1; i >= 0; i--)
                target.RemoveAt(i);
        }
    }

    [Fact]
    public void Synchronous_Item_Property_Change_Allocates_Only_Event_Args()
    {
        CountingNotifyItem item = new();
        ObservableList<CountingNotifyItem> list = new([item]);
        list.ItemPropertyChanged += (_, _) => { };
        PropertyChangedEventArgs args = new("Value");
        item.Raise(args);

        const int Iterations = 100;
        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < Iterations; i++)
            item.Raise(args);
        long allocatedPerEvent = (GC.GetAllocatedBytesForCurrentThread() - before) / Iterations;

        Assert.True(allocatedPerEvent <= 32, $"Each item property change allocated {allocatedPerEvent} bytes.");
    }
#endif
}
