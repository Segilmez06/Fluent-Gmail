using Microsoft.Web.WebView2.WinForms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Gmail
{
    /// <summary>
    /// Interaction logic for WebApp.xaml
    /// </summary>
    public partial class WebApp : Window
    {
        public WebApp()
        {
            InitializeComponent();

            // WebView2 Core has initialized
            WebView.CoreWebView2InitializationCompleted += (sender, e) =>
            {

                // When document's title has changed set window title to document title
                WebView.CoreWebView2.DocumentTitleChanged += (s1, e1) => Title = WebView.CoreWebView2.DocumentTitle;

            };
        }
    }
}
