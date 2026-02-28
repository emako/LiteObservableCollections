using LiteObservableCollections;
using System.Windows;

namespace WpfApp;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        SynchronizationContextCollectionEventDispatcher.Current!.CaptureFromCurrentContext();
    }
}
