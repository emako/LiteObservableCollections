namespace LiteObservableCollections.Tests;

public class ItemPropertyChangedConcurrencyTests
{
    private const int Iterations = 2_000;

    [Fact]
    public void Concurrent_First_Subscriptions_Share_One_Item_Subscription()
    {
        int lost = 0;
        int ghost = 0;
        using ConcurrentRunner runner = new();

        for (int i = 0; i < Iterations; i++)
        {
            Person person = new();
            ObservableList<Person> list = new([person]);
            int first = 0;
            int second = 0;

            runner.Run(
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
        using ConcurrentRunner runner = new();

        for (int i = 0; i < Iterations; i++)
        {
            Person person = new();
            ObservableList<Person> list = new([person]);
            EventHandler<ItemPropertyChangedEventArgs<Person>> existing = (_, _) => { };
            list.ItemPropertyChanged += existing;
            int added = 0;

            runner.Run(
                () => list.ItemPropertyChanged -= existing,
                () => list.ItemPropertyChanged += (_, _) => added++);

            person.Name = "Ada";
            if (added != 1) lost++;
        }

        Assert.Equal(0, lost);
    }

    /// <summary>
    /// Runs two actions concurrently on two long-lived worker threads, one pair per <see cref="Run"/> call.
    /// </summary>
    private sealed class ConcurrentRunner : IDisposable
    {
        private readonly Barrier _barrier = new(3);
        private readonly Thread[] _workers;
        private Action? _first;
        private Action? _second;
        private Exception? _failure;
        private volatile bool _stopping;

        public ConcurrentRunner()
        {
            _workers = [StartWorker(() => _first), StartWorker(() => _second)];
        }

        public void Run(Action first, Action second)
        {
            _first = first;
            _second = second;
            _barrier.SignalAndWait();
            _barrier.SignalAndWait();

            Exception? failure = Interlocked.Exchange(ref _failure, null);
            if (failure != null)
                throw new InvalidOperationException("A concurrent action failed.", failure);
        }

        public void Dispose()
        {
            _stopping = true;
            _barrier.SignalAndWait();
            foreach (Thread worker in _workers)
                worker.Join();
            _barrier.Dispose();
        }

        private Thread StartWorker(Func<Action?> getAction)
        {
            Thread thread = new(() =>
            {
                while (true)
                {
                    _barrier.SignalAndWait();
                    if (_stopping) return;

                    try
                    {
                        getAction()!();
                    }
                    catch (Exception e)
                    {
                        Interlocked.CompareExchange(ref _failure, e, null);
                    }

                    _barrier.SignalAndWait();
                }
            })
            {
                IsBackground = true,
            };
            thread.Start();
            return thread;
        }
    }
}
