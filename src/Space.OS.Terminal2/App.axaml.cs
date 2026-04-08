using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;

namespace Space.OS.Terminal2;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            ConsoleLogCapture.Install();

            desktop.ShutdownMode = ShutdownMode.OnMainWindowClose;

            _ = ShowSplashThenMainAsync(desktop);
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static async Task ShowSplashThenMainAsync(IClassicDesktopStyleApplicationLifetime desktop)
    {
        SplashWindow? splash = null;
        try
        {
            splash = new SplashWindow();
            splash.Show();

            await Task.Delay(1700);

            var main = new MainWindow();
            desktop.MainWindow = main;
            main.Opacity = 0;
            main.Show();

            // Fade in main window
            var showAnim = new Animation
            {
                Duration = TimeSpan.FromMilliseconds(320),
                Children =
                {
                    new KeyFrame
                    {
                        Cue = new Cue(0),
                        Setters = { new Setter(Window.OpacityProperty, 0.0) }
                    },
                    new KeyFrame
                    {
                        Cue = new Cue(1),
                        Setters = { new Setter(Window.OpacityProperty, 1.0) }
                    }
                }
            };
            _ = showAnim.RunAsync(main);

            // Fade out splash
            var closeAnim = new Animation
            {
                Duration = TimeSpan.FromMilliseconds(320),
                Children =
                {
                    new KeyFrame
                    {
                        Cue = new Cue(0),
                        Setters = { new Setter(Window.OpacityProperty, 1.0) }
                    },
                    new KeyFrame
                    {
                        Cue = new Cue(1),
                        Setters = { new Setter(Window.OpacityProperty, 0.0) }
                    }
                }
            };
            await closeAnim.RunAsync(splash);
            splash.Close();
        }
        catch (Exception ex)
        {
            splash?.Close();
            var dialog = new Window
            {
                Title = "Space.OS.Terminal2",
                Width = 400,
                Height = 200,
                Content = new TextBlock
                {
                    Text = $"Startup failed: {ex.Message}",
                    Margin = new Thickness(20)
                }
            };
            dialog.Show();
        }
    }
}
