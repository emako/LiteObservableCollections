using System.Collections.Specialized;
using System.ComponentModel;
using LiteObservableCollections.Concurrent;

namespace LiteObservableCollections.Tests;

public class CollectionNotificationTests
{
    private static readonly string[] DictionaryProperties = ["Keys", "Values", "Count", "Item[]"];

    [Fact]
    public void ObservableDictionary_Raises_Expected_Notifications()
    {
        ObservableDictionary<string, int> dictionary = new();
        List<NotifyCollectionChangedEventArgs> events = CollectionChangeRecorder.Attach(dictionary);
        List<string?> properties = RecordProperties(dictionary);

        dictionary.Add("a", 1);
        dictionary.Add("b", 2);
        dictionary["b"] = 3;
        dictionary["c"] = 4;
        Assert.True(dictionary.Remove("b"));
        dictionary.Clear();

        Assert.Collection(events,
            e => AssertAdded(e, Pair("a", 1), 0),
            e => AssertAdded(e, Pair("b", 2), 1),
            e => AssertReplaced(e, Pair("b", 3), Pair("b", 2), 1),
            e => AssertAdded(e, Pair("c", 4), -1),
            e => AssertRemoved(e, Pair("b", 3), 1),
            AssertReset);
        Assert.Equal([.. Enumerable.Repeat(DictionaryProperties, 6).SelectMany(x => x)], properties);
    }

    [Fact]
    public void ObservableHashSet_Raises_Expected_Notifications()
    {
        ObservableHashSet<int> set = new();
        List<NotifyCollectionChangedEventArgs> events = CollectionChangeRecorder.Attach(set);
        List<string?> properties = RecordProperties(set);

        Assert.True(set.Add(1));
        Assert.False(set.Add(1));
        Assert.True(set.Remove(1));
        Assert.False(set.Remove(1));
        set.Clear();

        Assert.Collection(events,
            e => AssertAdded(e, 1, -1),
            e => AssertRemoved(e, 1, -1),
            AssertReset);
        Assert.Equal(["Count", "Count", "Count"], properties);
    }

    [Fact]
    public void ObservableQueue_And_ObservableStack_Raise_Expected_Notifications()
    {
        ObservableQueue<int> queue = new();
        List<NotifyCollectionChangedEventArgs> queueEvents = CollectionChangeRecorder.Attach(queue);
        queue.Enqueue(1);
        queue.Enqueue(2);
        Assert.Equal(1, queue.Dequeue());
        queue.Clear();

        Assert.Collection(queueEvents,
            e => AssertAdded(e, 1, 0),
            e => AssertAdded(e, 2, 1),
            e => AssertRemoved(e, 1, 0),
            AssertReset);

        ObservableStack<int> stack = new();
        List<NotifyCollectionChangedEventArgs> stackEvents = CollectionChangeRecorder.Attach(stack);
        stack.Push(1);
        stack.Push(2);
        Assert.Equal(2, stack.Pop());
        stack.Clear();

        Assert.Collection(stackEvents,
            e => AssertAdded(e, 1, 0),
            e => AssertAdded(e, 2, 1),
            e => AssertRemoved(e, 2, 0),
            AssertReset);
    }

    [Fact]
    public void ObservableViewList_Raises_Reset_And_Count_On_Source_Change()
    {
        ObservableList<int> source = [1, 2];
        using ObservableViewList<int> view = new(source);
        List<NotifyCollectionChangedEventArgs> events = CollectionChangeRecorder.Attach(view);
        List<string?> properties = RecordProperties(view);

        source.Add(3);

        AssertReset(Assert.Single(events));
        Assert.Equal(["Count"], properties);
    }

    [Fact]
    public void Concurrent_Lists_Raise_Expected_Notifications()
    {
        ConcurrentObservableCollection<int> collection = new();
        AssertListNotifications(collection, collection, collection.Move);

        ObservableConcurrentList<int> list = new();
        AssertListNotifications(list, list, list.Move);

        static void AssertListNotifications(IList<int> target, INotifyCollectionChanged notifier, Action<int, int> move)
        {
            List<NotifyCollectionChangedEventArgs> events = CollectionChangeRecorder.Attach(notifier);
            List<string?> properties = RecordProperties((INotifyPropertyChanged)notifier);

            target.Add(1);
            target.Add(2);
            target[1] = 3;
            move(0, 1);
            Assert.True(target.Remove(1));
            target.RemoveAt(0);
            target.Clear();

            Assert.Collection(events,
                e => AssertAdded(e, 1, 0),
                e => AssertAdded(e, 2, 1),
                e => AssertReplaced(e, 3, 2, 1),
                e =>
                {
                    Assert.Equal(NotifyCollectionChangedAction.Move, e.Action);
                    Assert.Equal(1, Assert.Single(e.NewItems!.Cast<int>()));
                    Assert.Equal(1, e.NewStartingIndex);
                    Assert.Equal(0, e.OldStartingIndex);
                },
                e => AssertRemoved(e, 1, 1),
                e => AssertRemoved(e, 3, 0),
                AssertReset);
            Assert.Equal(
                ["Count", "Item[]", "Count", "Item[]", "Item[]", "Item[]", "Count", "Item[]", "Count", "Item[]", "Count", "Item[]"],
                properties);
        }
    }

    [Fact]
    public void ObservableConcurrentDictionary_Raises_Expected_Notifications()
    {
        ObservableConcurrentDictionary<string, int> dictionary = new();
        List<NotifyCollectionChangedEventArgs> events = CollectionChangeRecorder.Attach(dictionary);
        List<string?> properties = RecordProperties(dictionary);

        dictionary.Add("a", 1);
        dictionary["a"] = 2;
        dictionary["b"] = 3;
        Assert.True(dictionary.Remove("a"));
        dictionary.Clear();

        Assert.Collection(events,
            e => AssertAdded(e, Pair("a", 1), -1),
            e => AssertReplaced(e, Pair("a", 2), Pair("a", 1), -1),
            e => AssertAdded(e, Pair("b", 3), -1),
            e => AssertRemoved(e, Pair("a", 2), -1),
            AssertReset);
        string[] indexerProperties = ["Item[]", "Keys", "Values", "Count"];
        Assert.Equal([.. DictionaryProperties, .. indexerProperties, .. indexerProperties, .. DictionaryProperties, .. DictionaryProperties], properties);
    }

    [Fact]
    public void Concurrent_Queue_And_Stack_Raise_Expected_Notifications()
    {
        ObservableConcurrentQueue<int> queue = new();
        List<NotifyCollectionChangedEventArgs> queueEvents = CollectionChangeRecorder.Attach(queue);
        queue.Enqueue(1);
        Assert.Equal(1, queue.Dequeue());
        queue.Clear();

        Assert.Collection(queueEvents,
            e => AssertAdded(e, 1, -1),
            e => AssertRemoved(e, 1, -1),
            AssertReset);

        ObservableConcurrentStack<int> stack = new();
        List<NotifyCollectionChangedEventArgs> stackEvents = CollectionChangeRecorder.Attach(stack);
        stack.Push(1);
        Assert.Equal(1, stack.Pop());
        stack.Clear();

        Assert.Collection(stackEvents,
            e => AssertAdded(e, 1, -1),
            e => AssertRemoved(e, 1, -1),
            AssertReset);
    }

    [Fact]
    public void Dispatched_Collections_Post_Notifications_Until_Delivered()
    {
        AssertPosted(new ObservableDictionary<int, int>(), (c, d) => c.EventDispatcher = d, c => c.Add(1, 1), expectedPosts: 5);
        AssertPosted(new ObservableHashSet<int>(), (c, d) => c.EventDispatcher = d, c => c.Add(1), expectedPosts: 2);
        AssertPosted(new ObservableQueue<int>(), (c, d) => c.EventDispatcher = d, c => c.Enqueue(1), expectedPosts: 2);
        AssertPosted(new ObservableStack<int>(), (c, d) => c.EventDispatcher = d, c => c.Push(1), expectedPosts: 2);

        ObservableList<int> source = [];
        using ObservableViewList<int> view = new(source);
        AssertPosted(view, (c, d) => c.EventDispatcher = d, _ => source.Add(1), expectedPosts: 2);

        static void AssertPosted<TCollection>(
            TCollection collection,
            Action<TCollection, ICollectionEventDispatcher> setDispatcher,
            Action<TCollection> mutate,
            int expectedPosts)
            where TCollection : INotifyCollectionChanged, INotifyPropertyChanged
        {
            RecordingEventDispatcher dispatcher = new(isCurrentContext: false);
            setDispatcher(collection, dispatcher);
            int delivered = 0;
            collection.CollectionChanged += (_, _) => delivered++;
            collection.PropertyChanged += (_, _) => delivered++;

            mutate(collection);
            Assert.Equal(0, delivered);
            Assert.Equal(expectedPosts, dispatcher.Posted.Count);

            dispatcher.Flush();
            Assert.Equal(expectedPosts, delivered);
        }
    }

    [Fact]
    public void Disabled_Notifications_Are_Not_Raised()
    {
        ObservableDictionary<int, int> dictionary = new() { IsNotifyEnabled = false };
        ConcurrentObservableCollection<int> collection = new() { IsNotifyEnabled = false };
        int raised = 0;
        dictionary.CollectionChanged += (_, _) => raised++;
        dictionary.PropertyChanged += (_, _) => raised++;
        collection.CollectionChanged += (_, _) => raised++;
        collection.PropertyChanged += (_, _) => raised++;

        dictionary.Add(1, 1);
        dictionary[1] = 2;
        dictionary.Remove(1);
        collection.Add(1);
        collection.Clear();

        Assert.Equal(0, raised);
    }

#if NET
    [Fact]
    public void Mutations_Do_Not_Allocate_Without_Subscribers()
    {
        ObservableDictionary<int, int> dictionary = new(new Dictionary<int, int>(200));
        ObservableHashSet<int> set = new();
        ObservableQueue<int> queue = new(new Queue<int>(200));
        ObservableStack<int> stack = new(new Stack<int>(200));
        ConcurrentObservableCollection<int> concurrentCollection = new();
        ObservableConcurrentList<int> concurrentList = new();

        void AddAndRemoveAll()
        {
            for (int i = 0; i < 100; i++)
            {
                dictionary.Add(i, i);
                dictionary[i] = i + 1;
                set.Add(i);
                queue.Enqueue(i);
                stack.Push(i);
                concurrentCollection.Add(i);
                concurrentList.Add(i);
            }

            for (int i = 0; i < 100; i++)
            {
                dictionary.Remove(i);
                set.Remove(i);
                queue.Dequeue();
                stack.Pop();
                concurrentCollection.RemoveAt(concurrentCollection.Count - 1);
                concurrentList.RemoveAt(concurrentList.Count - 1);
            }
        }

        AddAndRemoveAll();
        long before = GC.GetAllocatedBytesForCurrentThread();
        AddAndRemoveAll();
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.Equal(0, allocated);
    }
#endif

    private static KeyValuePair<string, int> Pair(string key, int value) => new(key, value);

    private static List<string?> RecordProperties(INotifyPropertyChanged source)
    {
        List<string?> names = [];
        source.PropertyChanged += (_, e) => names.Add(e.PropertyName);
        return names;
    }

    private static void AssertAdded<T>(NotifyCollectionChangedEventArgs e, T item, int index)
    {
        Assert.Equal(NotifyCollectionChangedAction.Add, e.Action);
        Assert.Equal(item, Assert.Single(e.NewItems!.Cast<T>()));
        Assert.Equal(index, e.NewStartingIndex);
    }

    private static void AssertRemoved<T>(NotifyCollectionChangedEventArgs e, T item, int index)
    {
        Assert.Equal(NotifyCollectionChangedAction.Remove, e.Action);
        Assert.Equal(item, Assert.Single(e.OldItems!.Cast<T>()));
        Assert.Equal(index, e.OldStartingIndex);
    }

    private static void AssertReplaced<T>(NotifyCollectionChangedEventArgs e, T newItem, T oldItem, int index)
    {
        Assert.Equal(NotifyCollectionChangedAction.Replace, e.Action);
        Assert.Equal(newItem, Assert.Single(e.NewItems!.Cast<T>()));
        Assert.Equal(oldItem, Assert.Single(e.OldItems!.Cast<T>()));
        Assert.Equal(index, e.NewStartingIndex);
    }

    private static void AssertReset(NotifyCollectionChangedEventArgs e)
        => Assert.Equal(NotifyCollectionChangedAction.Reset, e.Action);
}
