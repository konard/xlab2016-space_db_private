using System.Configuration;
using System.Data;
using System.Windows;
using System.Windows.Media.Animation;

namespace Space.OS.Terminal;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private async void Application_Startup(object sender, StartupEventArgs e)
    {
        ConsoleLogCapture.Install();
        ShutdownMode = ShutdownMode.OnExplicitShutdown;
        SplashWindow? splash = null;
        try
        {
            splash = new SplashWindow();
            splash.Show();

            await Task.Delay(1700);

            var main = new MainWindow
            {
                Opacity = 0
            };
            MainWindow = main;
            main.Show();

            var showMainAnim = new DoubleAnimation(0, 1, new Duration(TimeSpan.FromMilliseconds(320)));
            main.BeginAnimation(Window.OpacityProperty, showMainAnim);

            var closeAnim = new DoubleAnimation(1, 0, new Duration(TimeSpan.FromMilliseconds(320)));
            splash.BeginAnimation(Window.OpacityProperty, closeAnim);
            await Task.Delay(330);
            splash.Close();

            ShutdownMode = ShutdownMode.OnMainWindowClose;
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Startup failed: {ex.Message}",
                "Space.OS.Terminal",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown(-1);
        }
    }
}

