namespace LiteObservableCollections.Tests;

public class ItemPropertyChangedConcurrencyTests
{
    private const int Iterations = 2_000;

    [Fact]
    public void Concurrent_First_Subscriptions_Share_One_Item_Subscription()
    {
        int lost = 0;
        int ghost = 0;

        for (int i = 0; i < Iterations; i++)
        {
            Person person = new();
            ObservableList<Person> list = new([person]);
            int first = 0;
            int second = 0;

            RunConcurrently(
                () => list.ItemPropertyChanged += (_, _) => first++,
                () => list.ItemPropertyChanged += (_, _) => second++);

            person.Name = "Ada";
            if (first != 1 || second != 1) lost++;

            list.Remove(person);
            person.Name = "Grace";
            if (first != 1 || second != 1) ghost++;
        }

        Assert.Equal(0, lost);
        Assert.Equal(0, ghost);
    }

    [Fact]
    public void Removing_Last_Handler_Concurrently_With_Adding_Does_Not_Lose_Handler()
    {
        int lost = 0;

        for (int i = 0; i < Iterations; i++)
        {
            Person person = new();
            ObservableList<Person> list = new([person]);
            EventHandler<ItemPropertyChangedEventArgs<Person>> existing = (_, _) => { };
            list.ItemPropertyChanged += existing;
            int added = 0;

            RunConcurrently(
                () => list.ItemPropertyChanged -= existing,
                () => list.ItemPropertyChanged += (_, _) => added++);

            person.Name = "Ada";
            if (added != 1) lost++;
        }

        Assert.Equal(0, lost);
    }

    private static void RunConcurrently(Action first, Action second)
    {
        using ManualResetEventSlim start = new();
        Thread firstThread = new(() => { start.Wait(); first(); });
        Thread secondThread = new(() => { start.Wait(); second(); });
        firstThread.Start();
        secondThread.Start();
        start.Set();
        firstThread.Join();
        secondThread.Join();
    }
}
