using System.Collections.Specialized;
using LiteObservableCollections.ComponentModel;

namespace LiteObservableCollections.Tests;

internal sealed class Person : ObservableObject
{
    private string _name = string.Empty;
    private int _age;

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    public int Age
    {
        get => _age;
        set => SetProperty(ref _age, value);
    }
}

/// <summary>
/// Captures posted/sent actions and can optionally run them immediately.
/// </summary>
internal sealed class RecordingEventDispatcher : ICollectionEventDispatcher
{
    private readonly List<Action> _posted = [];

    public RecordingEventDispatcher(bool isCurrentContext = true, bool runPostedImmediately = false)
    {
        IsCurrentContext = isCurrentContext;
        RunPostedImmediately = runPostedImmediately;
    }

    public bool IsCurrentContext { get; set; }

    public bool RunPostedImmediately { get; set; }

    public IReadOnlyList<Action> Posted => _posted;

    public void Post(Action action)
    {
        if (action == null) return;
        _posted.Add(action);
        if (RunPostedImmediately)
            action();
    }

    public void Send(Action action)
    {
        action?.Invoke();
    }

    public void Flush()
    {
        Action[] actions = [.. _posted];
        _posted.Clear();
        foreach (Action action in actions)
            action();
    }
}

internal static class CollectionChangeRecorder
{
    public static List<NotifyCollectionChangedEventArgs> Attach(INotifyCollectionChanged source)
    {
        List<NotifyCollectionChangedEventArgs> events = [];
        source.CollectionChanged += (_, e) => events.Add(e);
        return events;
    }
}
