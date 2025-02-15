using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.Web.WebView2.Core;
using System.IO;
using System.Reflection;

namespace Gmail;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Wpf.Ui.Controls.FluentWindow
{
    private DispatcherTimer StateTimer = new();

    string[] StyleSheetFiles = ["Debloat", "Mica"];
    string StyleSheetResourceNamespace = "Gmail.StyleSheets";
    string CustomStyleSheet = "";

    public MainWindow()
    {
        InitializeComponent();

        KeyUp += MainWindow_KeyUp;

        App_AccountButton.Visibility = Visibility.Hidden;
        App_PanelToggleButton.Visibility = Visibility.Hidden;
        App_SettingsButton.Visibility = Visibility.Hidden;

        WebView.Visibility = Visibility.Hidden;
        WebView.NavigationCompleted += WebView_NavigationCompleted;
        WebView.NavigationStarting += CheckForActivatingButtons;
        WebView.CoreWebView2InitializationCompleted += WebView_CoreWebView2InitializationCompleted;
        WebView.CoreWebView2InitializationCompleted += AttachSyncLoops;
        WebView.CoreWebView2InitializationCompleted += AttachHeaderButtonChecks;
        WebView.ContextMenuOpening += DisableBrowserContextMenu;

        DraggableElement.MouseLeftButtonDown += DraggableElement_MouseLeftButtonDown;
        DraggableElement.MouseMove += DraggableElement_MouseMove;
        DraggableElement.MouseWheel += DraggableElement_MouseWheel;


        App_PanelToggleButton.Click += ToggleLeftPane;
        App_SettingsButton.Click += ToggleSettingsPane;
        App_AccountButton.Click += ToggleAccountPane;
        HeaderIcon.MouseLeftButtonUp += HeaderIcon_MouseLeftButtonUp;

        Loaded += PlayGmailAnimation;



        Assembly CurrentAsm = Assembly.GetExecutingAssembly();
        foreach (string StyleSheet in StyleSheetFiles)
        {
            Stream? ResourceStream = CurrentAsm.GetManifestResourceStream($"{StyleSheetResourceNamespace}.{StyleSheet}.css");
            if (ResourceStream != null)
            {
                StreamReader Reader = new(ResourceStream);
                string RawContent = Reader.ReadToEnd();
                CustomStyleSheet += RawContent.Replace(";", " !important ;");
            }
        }



        StateTimer.Interval = TimeSpan.FromSeconds(0.01);
        StateTimer.Tick += CheckPanelState;



        HeaderIcon.Source = LoadEmbeddedImage("Gmail.Resources.gmail.png");
    }


    #region Developer Tools
    private void MainWindow_KeyUp(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.F12)
        {
            WebView.CoreWebView2.OpenDevToolsWindow();
        }
    }
    #endregion


    #region Header Buttons Visibility Check
    private void AttachHeaderButtonChecks(object? sender, CoreWebView2InitializationCompletedEventArgs e)
    {
        WebView.CoreWebView2.SourceChanged += CheckButtonsOnSourceChanged;
    }

    private void CheckButtonsOnSourceChanged(object? sender, CoreWebView2SourceChangedEventArgs e)
    {
        CheckForButtonState();
    }

    private void CheckForActivatingButtons(object? sender, CoreWebView2NavigationStartingEventArgs e)
    {
        CheckForButtonState();
    }

    private void CheckForButtonState()
    {
        if (WebView.CoreWebView2.Source.StartsWith("https://mail.google.com"))
        {
            App_AccountButton.Visibility = Visibility.Visible;
            App_PanelToggleButton.Visibility = Visibility.Visible;
            App_SettingsButton.Visibility = Visibility.Visible;
        }
        else
        {
            App_AccountButton.Visibility = Visibility.Hidden;
            App_PanelToggleButton.Visibility = Visibility.Hidden;
            App_SettingsButton.Visibility = Visibility.Hidden;
        }
    }
    #endregion


    #region Load Image From Assembly
    public BitmapImage LoadEmbeddedImage(string resourceName)
    {
        Assembly CurrentAsm = Assembly.GetExecutingAssembly();
        using Stream? stream = CurrentAsm.GetManifestResourceStream(resourceName);
            if (stream == null) return null;

        var image = new BitmapImage();
        image.BeginInit();
        image.StreamSource = stream;
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.EndInit();
        image.Freeze();

        return image;
    }
    #endregion


    #region Startup Animation
    private async void PlayGmailAnimation(object sender, RoutedEventArgs e)
    {
        await Task.Delay(150);
        AnimationViewer.PlayAnimation();
    }
    #endregion


    #region Disable Context Menu
    private void DisableBrowserContextMenu(object sender, ContextMenuEventArgs e)
    {
        e.Handled = true;
    }
    #endregion


    #region Sync State
    private async void CheckPanelState(object? sender, EventArgs e)
    {
        string CommandResult = await WebView.ExecuteScriptAsync("document.querySelector('div.aeN.WR.baA.nH.oy8Mbf').classList.length");
        if (CommandResult == "5")
        {
            App_PanelToggleIcon.Filled = true;
        }
        else if (CommandResult == "6" || CommandResult == "7")
        {
            App_PanelToggleIcon.Filled = false;
        }
    }
    private void AttachSyncLoops(object? sender, CoreWebView2InitializationCompletedEventArgs e)
    {
        WebView.CoreWebView2.DocumentTitleChanged += TitleChanged;
    }
    private void TitleChanged(object? sender, object e)
    {
        Title = WebView.CoreWebView2.DocumentTitle;
    }
    #endregion


    #region Title Bar Buttons Callback
    private void ToggleLeftPane(object sender, RoutedEventArgs e)
    {
        WebView.ExecuteScriptAsync("document.querySelector('div[role=\"button\"][aria-label*=\"Main menu\"]').click()\r\n");
    }
    private void ToggleSettingsPane(object sender, RoutedEventArgs e)
    {
        WebView.ExecuteScriptAsync("document.querySelector('a[role=\"button\"][aria-label=\"Settings\"]').click();");
    }
    private void ToggleAccountPane(object sender, RoutedEventArgs e)
    {
        WebView.ExecuteScriptAsync("document.querySelector('a[role=\"button\"][aria-label*=\"Google\"]:has(img)').click();");
    }
    private void HeaderIcon_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        WebView.Source = new("https://mail.google.com/mail/u/0/#inbox");
    }
    #endregion


    #region Window Controlling
    private void DraggableElement_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (e.Delta > 0)
        {
            if (WindowState == WindowState.Normal)
            {
                WindowState = WindowState.Maximized;
            }
        }
        else
        {
            if (WindowState == WindowState.Maximized)
            {
                WindowState = WindowState.Normal;
            }
            else if (WindowState == WindowState.Normal)
            {
                WindowState = WindowState.Minimized;
            }
        }
    }

    private void DraggableElement_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            if (WindowState == WindowState.Maximized)
                WindowState = WindowState.Normal;
            else
                WindowState = WindowState.Maximized;
        }
        else if (e.ClickCount == 1){
            DragMove();
        }
    }
    private void DraggableElement_MouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            var mousePosition = Mouse.GetPosition(null);

            double newLeft = mousePosition.X - Width / 2;
            double newTop = mousePosition.Y - Height / 2;

            var screenWidth = SystemParameters.PrimaryScreenWidth;
            var screenHeight = SystemParameters.PrimaryScreenHeight;

            if (newLeft < 0) newLeft = 0;
            if (newTop < 0) newTop = 0;
            if (newLeft + Width > screenWidth) newLeft = screenWidth - Width;
            if (newTop + Height > screenHeight) newTop = screenHeight - Height;

            WindowState = WindowState.Normal;

            Left = newLeft;
            Top = newTop;

            DragMove();
        }
    }
    #endregion


    #region Page Initialized
    private async void WebView_NavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
    {
        LoadingAnimation.Visibility = Visibility.Hidden;

        WebView.Visibility = Visibility.Visible;
        WebView.InvalidateVisual();
        WebView.UpdateLayout();
        StateTimer.Start();

        if (WebView.CoreWebView2.Source.StartsWith("https://mail.google.com"))
        {
            string RawData = await WebView.ExecuteScriptAsync("document.querySelector('img.gb_P.gbii').src;");
            if (RawData != null)
            {
                string ProfileImageURL = RawData.Split('"')[1];

                ImageBrush ProfileImageBrush = new()
                {
                    Stretch = Stretch.UniformToFill,
                    ImageSource = new BitmapImage(new Uri(ProfileImageURL))
                };

                App_AccountIcon.Visibility = Visibility.Hidden;
                App_AccountImageBorder.Background = ProfileImageBrush;
            }
        }
    }
    #endregion


    #region Inject CSS
    private void WebView_CoreWebView2InitializationCompleted(object? sender, CoreWebView2InitializationCompletedEventArgs e)
    {
        WebView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync($@"
            (function waitForHead() {{
                if (document.head) {{
                    window.styleElement = document.createElement(""style"");
                    window.styleElement.textContent = `{CustomStyleSheet}`;
                    document.head.appendChild(window.styleElement);
                }} else {{
                    requestAnimationFrame(waitForHead);
                }}
            }})();
        ");
    }
    #endregion
}