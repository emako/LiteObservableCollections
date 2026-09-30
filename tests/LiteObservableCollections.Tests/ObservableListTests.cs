using System.Collections.Specialized;

namespace LiteObservableCollections.Tests;

public class ObservableListTests
{
    [Fact]
    public void Add_Raises_CollectionChanged_And_Updates_Count()
    {
        ObservableList<int> list = new();
        List<NotifyCollectionChangedEventArgs> events = CollectionChangeRecorder.Attach(list);

        list.Add(1);

        Assert.Equal(new[] { 1 }, list);
        Assert.Single(events);
        Assert.Equal(NotifyCollectionChangedAction.Add, events[0].Action);
        Assert.Equal(1, events[0].NewItems![0]);
    }

    [Fact]
    public void AddRange_Raises_Reset_By_Default()
    {
        ObservableList<int> list = new();
        List<NotifyCollectionChangedEventArgs> events = CollectionChangeRecorder.Attach(list);

        list.AddRange([1, 2, 3]);

        Assert.Equal(new[] { 1, 2, 3 }, list);
        Assert.Single(events);
        Assert.Equal(NotifyCollectionChangedAction.Reset, events[0].Action);
    }

    [Fact]
    public void AddRange_Raises_Per_Item_When_IsNotifyOnEachInRange()
    {
        ObservableList<int> list = new() { IsNotifyOnEachInRange = true };
        List<NotifyCollectionChangedEventArgs> events = CollectionChangeRecorder.Attach(list);

        list.AddRange([1, 2]);

        Assert.Equal(2, events.Count);
        Assert.All(events, e => Assert.Equal(NotifyCollectionChangedAction.Add, e.Action));
    }

    [Fact]
    public void IsNotifyEnabled_Suppresses_CollectionChanged()
    {
        ObservableList<int> list = new();
        List<NotifyCollectionChangedEventArgs> events = CollectionChangeRecorder.Attach(list);

        list.IsNotifyEnabled = false;
        list.Add(1);
        list.IsNotifyEnabled = true;
        list.Add(2);

        Assert.Equal(new[] { 1, 2 }, list);
        Assert.Single(events);
        Assert.Equal(2, events[0].NewItems![0]);
    }

    [Fact]
    public void Remove_Clear_Reset_And_Move_Work()
    {
        ObservableList<string> list = new(["a", "b", "c"]);
        List<NotifyCollectionChangedEventArgs> events = CollectionChangeRecorder.Attach(list);

        Assert.True(list.Remove("b"));
        Assert.Equal(new[] { "a", "c" }, list);

        list.Move(0, 1);
        Assert.Equal(new[] { "c", "a" }, list);
        Assert.Equal(NotifyCollectionChangedAction.Move, events.Last().Action);

        list.Reset(["x"]);
        Assert.Equal(new[] { "x" }, list);
        Assert.Equal(NotifyCollectionChangedAction.Reset, events.Last().Action);

        list.Clear();
        Assert.Empty(list);
    }

    [Fact]
    public void Sync_Preserves_Existing_Items_At_Matching_Indexes()
    {
        Person first = new() { Name = "Ada" };
        Person second = new() { Name = "Grace" };
        ObservableList<Person> list = new([first, second]);

        Person incomingFirst = new() { Name = "Ada2" };
        Person incomingSecond = new() { Name = "Jean" };
        Person incomingThird = new() { Name = "Zoe" };

        list.Sync([incomingFirst, incomingSecond, incomingThird], (_, target, source) =>
        {
            target.Name = source.Name;
            target.Age = source.Age;
        });

        Assert.Equal(3, list.Count);
        Assert.Same(first, list[0]);
        Assert.Equal("Ada2", list[0].Name);
        Assert.Same(second, list[1]);
        Assert.Equal("Jean", list[1].Name);
        Assert.Same(incomingThird, list[2]);
    }

    [Fact]
    public void EventDispatcher_Posts_CollectionChanged_Off_Context()
    {
        ObservableList<int> list = new();
        RecordingEventDispatcher dispatcher = new(isCurrentContext: false);
        list.EventDispatcher = dispatcher;

        int raised = 0;
        list.CollectionChanged += (_, _) => raised++;

        list.Add(1);
        Assert.Equal(0, raised);
        Assert.Single(dispatcher.Posted);

        dispatcher.Flush();
        Assert.Equal(1, raised);
    }
}
