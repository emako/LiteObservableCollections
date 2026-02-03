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
    [ObservableProperty]
    public partial string? Name { get; set; }

    [RelayCommand]
    private void RemoveList()
    {
        MainViewModel.List.Remove(this);
    }

    public override string? ToString() => Name;
}

public partial class MainViewModel : ObservableObject
{
    public static ObservableList<TestItem> List { get; protected set; } = [];

    public ObservableDictionary<int, TestItem> Dict { get; protected set; } = [];

    public ObservableHashSet<TestItem> HashSet { get; protected set; } = [];

    public ObservableQueue<TestItem> Queue { get; protected set; } = [];

    public ObservableStack<TestItem> Stack { get; protected set; } = [];

    private int _dictKey = 1;
    private int _suffix = 1;

    public MainViewModel()
    {
        List = new([new TestItem() { Name = "initial" }]);
    }

    [RelayCommand]
    private void AddList()
    {
        List.Add(new TestItem() { Name = $"ListItem {_suffix++}" });
    }

    [RelayCommand]
    private void RemoveList()
    {
        if (List.Count != 0)
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
