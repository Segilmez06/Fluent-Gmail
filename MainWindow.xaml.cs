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
    #region Panel state timer

    // Timer to sync left panel state with icon
    private DispatcherTimer StateTimer = new()
    {
        Interval = TimeSpan.FromSeconds(0.01),
    };

    #endregion


    #region Integrations

    // Namespace that stores the integration stylesheets
    readonly string IntegrationNamespace = "Gmail.Integrations";

    // Available integrations - BEWARE! Not all integrations use stylesheets!
    readonly string[] AvailableIntegrations = [
        "fix-account-popup",
        "fix-filter-box",
        "fix-gaps",
        "mica",
        "mica-login",
        "native-controls",
        "native-logo",
        "smooth-reveal",
        "wheel-window-state",
        "disable-system-context",
        "devtools"
        ];

    // List of enabled integrations
    List<string> EnabledIntegrations = [];

    // Simple wrapper function to check if integration is enabled
    bool IsIntegrationEnabled(string IntegrationName) => EnabledIntegrations.Contains(IntegrationName);

    #endregion


    #region File data

    // Config file relative path from working directory
    readonly string ConfigPath = "config.ini";

    // Custom CSS file relative path from working directory
    readonly string CustomCSSPath = "Custom.css";


    // Config file raw data
    string Config = "";

    // Custom CSS file raw data
    string CustomCSS = "";

    #endregion


    #region Final stylesheet

    // Final CSS to get injected into page
    string FinalCSS = "";

    #endregion


    public MainWindow()
    {
        #region Reading and injecting custom style

        // If user created a custom CSS file
        if (File.Exists(CustomCSSPath))
        {
            // Read all content to inject later
            CustomCSS = File.ReadAllText(CustomCSSPath);

            FinalCSS += CustomCSS.Replace(";", " !important ;");
        }

        #endregion


        #region Reading config and fetching integrations

        // Enable all integrations, some might be disabled with config
        EnabledIntegrations.AddRange(AvailableIntegrations);

        // If user created a config file
        if (File.Exists(ConfigPath))
        {
            // Read all config to parse
            Config = File.ReadAllText(ConfigPath);

            // Iterate all lines
            foreach (string Line in Config.Replace(' ', '\0').Split("\n"))
            {
                // Skip comments
                if (!Line.Contains('=') || Line.StartsWith('#') || Line.StartsWith(';') || Line.Length < 2)
                    continue;

                // Key key-value pair
                string[] Statements = Line.Trim().Split('=');
                (string ConfigKey, string ConfigValue) = (Statements[0], Statements[1]);

                // Disable integration
                if (AvailableIntegrations.Contains(ConfigKey) && (ConfigValue.ToLower() == "false" || ConfigValue == "0"))
                    EnabledIntegrations.Remove(ConfigKey);
            }
        }

        // Get active assembly to extract embedded resources
        Assembly CurrentAsm = Assembly.GetExecutingAssembly();

        // Iterate through enabled integrations
        foreach (string Integration in EnabledIntegrations)
        {
            // Get resource from assembly
            Stream? ResourceStream = CurrentAsm.GetManifestResourceStream($"{IntegrationNamespace}.{Integration}.css");

            // If file is available
            if (ResourceStream != null)
            {
                // Create a stream reader to fetch data from file
                StreamReader Reader = new(ResourceStream);

                // Get raw content from file
                string RawContent = Reader.ReadToEnd();

                // Elevate CSS lines to override Google's dedfault styles
                FinalCSS += RawContent.Replace(";", " !important ;");
            }
        }

        #endregion


        #region Initialize window

        // Initialize window
        InitializeComponent();

        #endregion


        #region Hide controls

        // Hide native controls
        App_AccountButton.Visibility = Visibility.Hidden;
        App_PanelToggleButton.Visibility = Visibility.Hidden;
        App_SettingsButton.Visibility = Visibility.Hidden;


        // Hide WebView
        WebView.Visibility = Visibility.Hidden;

        // Hide header brandings
        HeaderIcon.Visibility = Visibility.Hidden;
        HeaderText.Visibility = Visibility.Hidden;

        #endregion


        #region Bind listeners

        // Initialize listener for F12 key
        if (!IsIntegrationEnabled("devtools"))
            WebView.CoreWebView2InitializationCompleted += AttachDevToolsManager;


        // Listen for profile image
        WebView.NavigationCompleted += WebView_NavigationCompleted;

        // Check if showing native controls needed
        if (IsIntegrationEnabled("native-controls"))
            WebView.NavigationStarting += CheckForButtonState;

        // Inject CSS
        WebView.CoreWebView2InitializationCompleted += WebView_CoreWebView2InitializationCompleted;
        
        // Attach cross-env sync listeners (e.g. title listener)
        WebView.CoreWebView2InitializationCompleted += AttachSyncLoops;

        // Attach button checkers on URL change
        if (IsIntegrationEnabled("native-controls"))
            WebView.CoreWebView2InitializationCompleted += AttachHeaderButtonChecks;

        // Disable MS-Edge default context menu
        if (IsIntegrationEnabled("disable-system-context"))
            WebView.CoreWebView2InitializationCompleted += AttachContextMenuDisable; ;

        // Listen for drag and double-click to maximize
        DraggableElement.MouseLeftButtonDown += DraggableElement_MouseLeftButtonDown;

        // Listen for drag while maximized
        DraggableElement.MouseMove += DraggableElement_MouseMove;

        // Listen for maximize-normalize-minimize with mouse scroll wheel
        if(IsIntegrationEnabled("wheel-window-state"))
            DraggableElement.MouseWheel += DraggableElement_MouseWheel;


        // Check for left panel state
        StateTimer.Tick += CheckPanelState;

        #endregion


        #region Bind button callbacks

        // Header button callbacks
        App_PanelToggleButton.Click += ToggleLeftPane;
        App_SettingsButton.Click += ToggleSettingsPane;
        App_AccountButton.Click += ToggleAccountPane;
        HeaderIcon.MouseLeftButtonUp += HeaderIcon_MouseLeftButtonUp;

        #endregion


        #region Play animation

        // Play loading animation
        Loaded += PlayGmailAnimation;

        #endregion


        #region Header branding

        if (IsIntegrationEnabled("native-logo"))
        {
            // Show elements
            HeaderIcon.Visibility = Visibility.Visible;
            HeaderText.Visibility = Visibility.Visible;


            // Get image as stream
            using (Stream? ImageStream = CurrentAsm.GetManifestResourceStream("Gmail.Resources.gmail.png"))
            {
                // If image is available
                if (ImageStream != null)
                {
                    // Create new Bitmap
                    BitmapImage Image = new();

                    // Read data from stream
                    Image.BeginInit();
                    Image.StreamSource = ImageStream;
                    Image.CacheOption = BitmapCacheOption.OnLoad;

                    // End reading
                    Image.EndInit();
                    Image.Freeze();

                    // Set source to image
                    HeaderIcon.Source = Image;
                }
            }
        }

        #endregion
    }


    #region F12 DevTools

    private void AttachDevToolsManager(object? sender, CoreWebView2InitializationCompletedEventArgs e)
    {
        // Disable DevTools
        WebView.CoreWebView2.Settings.AreDevToolsEnabled = false;
    }

    #endregion


    #region Header Buttons Visibility

    private void AttachHeaderButtonChecks(object? sender, CoreWebView2InitializationCompletedEventArgs e)
    {
        if (IsIntegrationEnabled("native-controls"))
            WebView.CoreWebView2.SourceChanged += CheckForButtonState;
    }

    private void CheckForButtonState(object? sender, object e)
    {
        // If currently at mail view (not login, etc.)
        if (WebView.CoreWebView2.Source.StartsWith("https://mail.google.com"))
        {
            // Show all buttons
            App_AccountButton.Visibility = Visibility.Visible;
            App_PanelToggleButton.Visibility = Visibility.Visible;
            App_SettingsButton.Visibility = Visibility.Visible;
        }
        else
        {
            // Hide all buttosn
            App_AccountButton.Visibility = Visibility.Hidden;
            App_PanelToggleButton.Visibility = Visibility.Hidden;
            App_SettingsButton.Visibility = Visibility.Hidden;
        }
    }

    #endregion


    #region Startup Animation

    private async void PlayGmailAnimation(object sender, RoutedEventArgs e)
    {
        // Wait a few before starting animation
        await Task.Delay(150);

        // Start the animation
        AnimationViewer.PlayAnimation();
    }

    #endregion


    #region Disable Context Menu

    private void AttachContextMenuDisable(object? sender, CoreWebView2InitializationCompletedEventArgs e)
    {
        // When context menu requested
        WebView.CoreWebView2.ContextMenuRequested += (s, e) =>
        {
            // Return true
            e.Handled = true;
        };
    }

    #endregion


    #region Sync State

    // Fired by timer
    private async void CheckPanelState(object? sender, EventArgs e)
    {
        // Get panel state by executing JS
        string CommandResult = await WebView.ExecuteScriptAsync("document.querySelector('div.aeN.WR.baA.nH.oy8Mbf').classList.length");

        // If panel is expanded
        if (CommandResult == "5")
        {
            // Set icon to filled
            App_PanelToggleIcon.Filled = true;
        }

        // If panel is collapsed
        else if (CommandResult == "6" || CommandResult == "7")
        {
            // Set icon to outline
            App_PanelToggleIcon.Filled = false;
        }
    }

    private void AttachSyncLoops(object? sender, CoreWebView2InitializationCompletedEventArgs e)
    {
        // Attach listener for document title change
        WebView.CoreWebView2.DocumentTitleChanged += TitleChanged;
    }

    private void TitleChanged(object? sender, object e)
    {
        // Set window title to document title
        Title = WebView.CoreWebView2.DocumentTitle;
    }

    #endregion


    #region Title Bar Buttons Callback

    private void ToggleLeftPane(object sender, RoutedEventArgs e)
    {
        // Perform click on left pane toggle button (stack - burger icon)
        WebView.ExecuteScriptAsync("document.querySelector('div[role=\"button\"][aria-label*=\"Main menu\"]').click()\r\n");
    }

    private void ToggleSettingsPane(object sender, RoutedEventArgs e)
    {
        // Perform click on settings pane toggle button (gear icon)
        WebView.ExecuteScriptAsync("document.querySelector('a[role=\"button\"][aria-label=\"Settings\"]').click();");
    }

    private void ToggleAccountPane(object sender, RoutedEventArgs e)
    {
        // Perform click on account pane toggle button (profile picture)
        WebView.ExecuteScriptAsync("document.querySelector('a[role=\"button\"][aria-label*=\"Google\"]:has(img)').click();");
    }

    private void HeaderIcon_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        // When mouse clicked header icon, go to inbox view
        WebView.Source = new("https://mail.google.com/mail/u/0/#inbox");
    }

    #endregion


    #region Window Controlling

    // Listen for mouse wheel movements
    private void DraggableElement_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        // If positive direction
        if (e.Delta > 0)
        {
            // If window state is normal, then maximize
            if (WindowState == WindowState.Normal)
            {
                WindowState = WindowState.Maximized;
            }
        }
        // If negative direction
        else
        {
            // If window state is maximized, then normalize
            if (WindowState == WindowState.Maximized)
            {
                WindowState = WindowState.Normal;
            }
            // If window state is normal, then minimize
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
    
    // When page loading completed - GMail is ready to use
    private async void WebView_NavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
    {
        // Hide loading animation
        LoadingAnimation.Visibility = Visibility.Hidden;


        // Show WebView and re-render
        WebView.Visibility = Visibility.Visible;
        WebView.InvalidateVisual();
        WebView.UpdateLayout();


        // Start checking for left pane state
        StateTimer.Start();


        // If currently at mail view (not login, etc.)
        if (WebView.CoreWebView2.Source.StartsWith("https://mail.google.com"))
        {
            // Fetch raw data from profile image
            string RawData = await WebView.ExecuteScriptAsync("document.querySelector('img.gb_P.gbii').src;");
            
            // If profile image is available
            if (RawData != null)
            {
                // Extract URL from raw data
                string ProfileImageURL = RawData.Split('"')[1];

                // Create a new brush with URL
                ImageBrush ProfileImageBrush = new()
                {
                    Stretch = Stretch.UniformToFill,
                    ImageSource = new BitmapImage(new Uri(ProfileImageURL))
                };


                // Hide placeholder account icon
                App_AccountIcon.Visibility = Visibility.Hidden;

                // Set profile picture
                App_AccountImageBorder.Background = ProfileImageBrush;
            }
        }
    }

    #endregion


    #region Inject CSS

    private void WebView_CoreWebView2InitializationCompleted(object? sender, CoreWebView2InitializationCompletedEventArgs e)
    {
        // Wait while document head to be available, then inject stylesheets
        WebView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync($@"
            (function waitForHead() {{
                if (document.head) {{
                    window.styleElement = document.createElement(""style"");
                    window.styleElement.textContent = `{FinalCSS}`;
                    document.head.appendChild(window.styleElement);
                }} else {{
                    requestAnimationFrame(waitForHead);
                }}
            }})();
        ");
    }

    #endregion
}