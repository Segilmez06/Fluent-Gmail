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

        string[] CmdlineArgs = Environment.GetCommandLineArgs();

        if (CmdlineArgs.Contains("--pwa"))
        {
            new WebApp().Show();
        }
        else
        {
            new MainWindow().Show();
        }
    }
}

