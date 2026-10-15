using System.Collections.Specialized;

namespace LiteObservableCollections.Tests;

public class ObservableViewListTests
{
    [Fact]
    public void View_Applies_Filter_Sort_And_Projection()
    {
        ObservableList<Person> source =
        [
            new Person { Name = "Ada", Age = 30 },
            new Person { Name = "Grace", Age = 20 },
            new Person { Name = "Jean", Age = 40 },
        ];

        using ObservableViewList<Person, string> view = new(source, p => p.Name);
        view.AttachFilter(p => p.Age >= 30);
        view.AttachSort((a, b) => string.CompareOrdinal(a, b));

        Assert.Equal(new[] { "Ada", "Jean" }, view);

        source.Add(new Person { Name = "Zoe", Age = 50 });
        Assert.Equal(new[] { "Ada", "Jean", "Zoe" }, view);
    }

    [Fact]
    public void View_Raises_CollectionChanged_On_Source_Change()
    {
        ObservableList<int> source = new([1, 2]);
        using ObservableViewList<int, int> view = new(source, x => x * 10);
        List<NotifyCollectionChangedEventArgs> events = CollectionChangeRecorder.Attach(view);

        source.Add(3);

        Assert.Equal(new[] { 10, 20, 30 }, view);
        Assert.NotEmpty(events);
    }

    [Fact]
    public void Dispose_Stops_Tracking_Source()
    {
        ObservableList<int> source = new([1]);
        ObservableViewList<int, int> view = new(source, x => x);
        view.Dispose();

        source.Add(2);
        Assert.Equal(new[] { 1 }, view);
    }
}
