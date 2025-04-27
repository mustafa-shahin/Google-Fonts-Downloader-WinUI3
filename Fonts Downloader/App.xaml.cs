using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using System.Threading.Tasks;
// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Fonts_Downloader
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        private Window m_window;

        public App()
        {
            this.InitializeComponent();

            // Set global exception handlers
            this.UnhandledException += App_UnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
        }

        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            m_window = new MainWindow();
            m_window.Activate();
        }

        private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            e.Handled = true;
            HandleUnhandledException(e.Exception);
        }

        private void CurrentDomain_UnhandledException(object sender, System.UnhandledExceptionEventArgs e)
        {
            HandleUnhandledException((Exception)e.ExceptionObject);
        }

        private void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            HandleUnhandledException(e.Exception);
            e.SetObserved(); // Mark as observed to prevent application termination
        }

        private async void HandleUnhandledException(Exception ex)
        {
            try
            {
                Logger.HandleError("Unhandled application exception", ex);
                ContentDialog dialog = new ContentDialog
                {
                    Title = "Application Error",
                    Content = "An unexpected error occurred. The application will now close.\n\n" +
                             "Please check the log file for details or report this issue.",
                    PrimaryButtonText = "OK",
                    XamlRoot = m_window.Content.XamlRoot
                };

                await dialog.ShowAsync();
            }
            catch
            {
                // Last resort if even the dialog fails
            }
            finally
            {
                // Terminate the app
                Application.Current.Exit();
            }
        }
    }
}