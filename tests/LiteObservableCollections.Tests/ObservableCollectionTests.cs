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

    [Fact]
    public void Remove_Uses_The_Stored_Instance_For_Equal_Items()
    {
        EqualPerson first = new(1);
        EqualPerson second = new(1);
        ObservableCollection<EqualPerson> collection = new([first, second]);

        object? removedItem = null;
        collection.CollectionChanged += (_, e) => removedItem = e.OldItems?[0];

        int raised = 0;
        collection.ItemPropertyChanged += (_, _) => raised++;

        Assert.True(collection.Remove(second));
        Assert.Same(first, removedItem);
        Assert.Same(second, Assert.Single(collection));

        first.Name = "removed";
        Assert.Equal(0, raised);

        second.Name = "retained";
        Assert.Equal(1, raised);
    }

    [Fact]
    public void RemoveRange_Unsubscribes_The_Stored_Instance_For_Equal_Items()
    {
        EqualPerson first = new(1);
        EqualPerson second = new(1);
        ObservableCollection<EqualPerson> collection = new([first, second]);

        int raised = 0;
        collection.ItemPropertyChanged += (_, _) => raised++;

        collection.RemoveRange([second]);

        first.Name = "removed";
        Assert.Equal(0, raised);

        second.Name = "retained";
        Assert.Equal(1, raised);
    }

    [Fact]
    public void ItemPropertyChanged_Tracks_Clear_Reset_And_Move()
    {
        Person first = new();
        Person second = new();
        ObservableCollection<Person> collection = new([first, second]);

        int raised = 0;
        collection.ItemPropertyChanged += (_, _) => raised++;

        collection.Move(0, 1);
        first.Name = "Moved";
        Assert.Equal(1, raised);

        collection.Clear();
        first.Name = "Cleared";
        second.Name = "Cleared";
        Assert.Equal(1, raised);

        collection.Reset([second]);
        first.Name = "Old";
        second.Name = "Reset";
        Assert.Equal(2, raised);
    }

    [Fact]
    public void ItemPropertyChanged_Tracks_Range_Operations_When_Notifying_Each_Item()
    {
        ObservableCollection<Person> collection = new()
        {
            IsNotifyOnEachInRange = true,
        };
        Person first = new();
        Person second = new();

        int raised = 0;
        collection.ItemPropertyChanged += (_, _) => raised++;

        collection.AddRange([first, second]);
        first.Name = "Ada";
        second.Name = "Grace";
        Assert.Equal(2, raised);

        collection.RemoveRange([first]);
        first.Name = "Old";
        second.Name = "Jean";
        Assert.Equal(3, raised);
    }
}
