using System.Collections.Specialized;

namespace LiteObservableCollections.Tests;

public class ObservableCollectionTests
{
    [Fact]
    public void AddRange_Raises_Reset_By_Default()
    {
        ObservableCollection<int> collection = new();
        List<NotifyCollectionChangedEventArgs> events = CollectionChangeRecorder.Attach(collection);

        collection.AddRange([1, 2, 3]);

        Assert.Equal(new[] { 1, 2, 3 }, collection);
        Assert.Single(events);
        Assert.Equal(NotifyCollectionChangedAction.Reset, events[0].Action);
    }

    [Fact]
    public void ItemPropertyChanged_Works_Across_AddRange_And_RemoveRange()
    {
        ObservableCollection<Person> collection = new();
        Person a = new();
        Person b = new();

        int raised = 0;
        collection.ItemPropertyChanged += (_, _) => raised++;

        collection.AddRange([a, b]);
        a.Name = "Ada";
        b.Name = "Grace";
        Assert.Equal(2, raised);

        collection.RemoveRange([a]);
        a.Name = "Old";
        Assert.Equal(2, raised);

        b.Age = 1;
        Assert.Equal(3, raised);
    }

    [Fact]
    public void IsNotifyEnabled_Still_Tracks_Item_Subscriptions()
    {
        ObservableCollection<Person> collection = new();
        int raised = 0;
        collection.ItemPropertyChanged += (_, _) => raised++;

        collection.IsNotifyEnabled = false;
        Person person = new();
        collection.Add(person);
        collection.IsNotifyEnabled = true;

        person.Name = "Ada";
        Assert.Equal(1, raised);
    }

    [Fact]
    public void Replace_Indexer_Updates_Item_Subscriptions()
    {
        ObservableCollection<Person> collection = new();
        Person oldItem = new();
        Person newItem = new();
        collection.Add(oldItem);

        int raised = 0;
        collection.ItemPropertyChanged += (_, e) =>
        {
            raised++;
            Assert.Same(newItem, e.Item);
        };

        collection[0] = newItem;
        oldItem.Name = "Old";
        Assert.Equal(0, raised);

        newItem.Name = "New";
        Assert.Equal(1, raised);
    }
}
