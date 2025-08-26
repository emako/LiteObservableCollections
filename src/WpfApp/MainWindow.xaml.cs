using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiteObservableCollections;
using System.Windows;

namespace WpfApp;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        DataContext = new MainViewModel();
        InitializeComponent();
    }
}

public partial class TestItem : ObservableObject
{
    public string? Name
    {
        get => field;
        set => SetProperty(ref field, value);
    }

    [RelayCommand]
    private void RemoveList()
    {
        MainViewModel.List.Remove(this);
    }

    public override string? ToString() => Name;
}

public partial class MainViewModel : ObservableObject
{
    public static ObservableList<TestItem> List { get; } = [];

    public ObservableDictionary<int, TestItem> Dict { get; } = [];

    public ObservableHashSet<TestItem> HashSet { get; } = [];

    public ObservableQueue<TestItem> Queue { get; } = [];

    public ObservableStack<TestItem> Stack { get; } = [];

    private int _dictKey = 1;
    private int _suffix = 1;

    [RelayCommand]
    private void AddList()
    {
        List.Add(new TestItem() { Name = $"ListItem {_suffix++}" });
    }

    [RelayCommand]
    private void RemoveList()
    {
        List.Remove(List.First());
    }

    [RelayCommand]
    private void AddDict()
    {
        Dict.Add(_dictKey, new TestItem() { Name = $"DictValue {_dictKey}" });
        _dictKey++;
    }

    [RelayCommand]
    private void AddHashSet()
    {
        HashSet.Add(new TestItem() { Name = $"HashSetItem {_suffix++}" });
    }

    [RelayCommand]
    private void AddQueue()
    {
        Queue.Enqueue(new TestItem() { Name = $"HashSetItem {_suffix++}" });
    }

    [RelayCommand]
    private void AddStack()
    {
        Stack.Push(new TestItem() { Name = $"StackItem {_suffix++}" });
    }
}
