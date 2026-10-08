using System.Collections.Specialized;
using System.ComponentModel;
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

internal sealed class EqualPerson(int id) : ObservableObject
{
    private string _name = string.Empty;

    public int Id { get; } = id;

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    public override bool Equals(object? obj) => obj is EqualPerson other && Id == other.Id;

    public override int GetHashCode() => Id;
}

internal sealed class NonStandardNotifyItem : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    public void RaiseWithNullSender(string propertyName)
        => PropertyChanged?.Invoke(null, new PropertyChangedEventArgs(propertyName));
}

internal sealed class CountingNotifyItem : INotifyPropertyChanged
{
    private PropertyChangedEventHandler? _propertyChanged;

    public int AddedHandlers { get; private set; }

    public int RemovedHandlers { get; private set; }

    public event PropertyChangedEventHandler? PropertyChanged
    {
        add
        {
            AddedHandlers++;
            _propertyChanged += value;
        }
        remove
        {
            RemovedHandlers++;
            _propertyChanged -= value;
        }
    }

    public void Raise(string propertyName)
        => Raise(new PropertyChangedEventArgs(propertyName));

    public void Raise(PropertyChangedEventArgs args)
        => _propertyChanged?.Invoke(this, args);
}

internal sealed class ThrowingNotifyItem : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged
    {
        add => throw new InvalidOperationException("Subscription rejected.");
        remove { }
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
