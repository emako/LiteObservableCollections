using System.Collections.Specialized;
using LiteObservableCollections.ComponentModel;
using LiteObservableCollections.EventListeners;

namespace LiteObservableCollections.Tests;

public class ObservableObjectTests
{
    [Fact]
    public void SetProperty_Raises_Changing_And_Changed()
    {
        Person person = new();
        List<string?> changing = [];
        List<string?> changed = [];

        person.PropertyChanging += (_, e) => changing.Add(e.PropertyName);
        person.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        person.Name = "Ada";
        person.Name = "Ada";

        Assert.Equal(new[] { nameof(Person.Name) }, changing);
        Assert.Equal(new[] { nameof(Person.Name) }, changed);
    }

    [Fact]
    public void IsNotifyEnabled_Suppresses_Property_Events()
    {
        Person person = new();
        int raised = 0;
        person.PropertyChanged += (_, _) => raised++;

        person.IsNotifyEnabled = false;
        person.Name = "Ada";
        Assert.Equal(0, raised);

        person.IsNotifyEnabled = true;
        person.Age = 1;
        Assert.Equal(1, raised);
    }
}

public class PropertyChangedEventListenerTests
{
    [Fact]
    public void RegisterHandler_Filters_By_Property_Name_And_Expression()
    {
        Person person = new();
        using PropertyChangedEventListener listener = new(person);

        int any = 0;
        int nameOnly = 0;
        int expressionOnly = 0;

        listener.RegisterHandler((_, _) => any++);
        listener.RegisterHandler(nameof(Person.Name), (_, _) => nameOnly++);
        listener.RegisterHandler(() => person.Name, (_, _) => expressionOnly++);

        person.Name = "Ada";
        person.Age = 20;

        Assert.Equal(2, any);
        Assert.Equal(1, nameOnly);
        Assert.Equal(1, expressionOnly);
    }
}

public class CollectionChangedEventListenerTests
{
    [Fact]
    public void RegisterHandler_Filters_By_Action()
    {
        ObservableList<int> list = new();
        using CollectionChangedEventListener listener = new(list);

        int addCount = 0;
        int resetCount = 0;
        listener.RegisterHandler(NotifyCollectionChangedAction.Add, (_, _) => addCount++);
        listener.RegisterHandler(NotifyCollectionChangedAction.Reset, (_, _) => resetCount++);

        list.Add(1);
        list.AddRange([2, 3]);

        Assert.Equal(1, addCount);
        Assert.Equal(1, resetCount);
    }
}
