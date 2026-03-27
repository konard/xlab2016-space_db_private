using System.Windows;
using System.Windows.Media.Animation;

namespace Space.OS.Terminal;

public partial class SplashWindow : Window
{
    public SplashWindow()
    {
        InitializeComponent();
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        Loaded += SplashWindow_Loaded;
    }

    private void SplashWindow_Loaded(object sender, RoutedEventArgs e)
    {
        var zoom = new DoubleAnimation(0.96, 1.0, new Duration(TimeSpan.FromMilliseconds(700)))
        {
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
        };
        SplashScale.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleXProperty, zoom);
        SplashScale.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleYProperty, zoom);

        var fade = new DoubleAnimation(0.0, 1.0, new Duration(TimeSpan.FromMilliseconds(520)));
        BeginAnimation(OpacityProperty, fade);
    }
}
