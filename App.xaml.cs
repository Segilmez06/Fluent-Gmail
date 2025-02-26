using System.Configuration;
using System.Data;
using System.Security.Policy;
using System.Windows;

namespace Gmail;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        Window TargetWindow = Environment.GetCommandLineArgs().Contains("--pwa") ? new WebApp() : new MainWindow();
        TargetWindow.Show();
    }
}

