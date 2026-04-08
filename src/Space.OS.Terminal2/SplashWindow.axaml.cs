using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Rendering.Composition;
using Avalonia.Styling;

namespace Space.OS.Terminal2;

public partial class SplashWindow : Window
{
    public SplashWindow()
    {
        InitializeComponent();
        Loaded += SplashWindow_Loaded;
    }

    private void SplashWindow_Loaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        // Fade in animation
        var fadeIn = new Animation
        {
            Duration = TimeSpan.FromMilliseconds(520),
            Children =
            {
                new KeyFrame
                {
                    Cue = new Cue(0),
                    Setters = { new Setter(OpacityProperty, 0.0) }
                },
                new KeyFrame
                {
                    Cue = new Cue(1),
                    Setters = { new Setter(OpacityProperty, 1.0) }
                }
            }
        };
        _ = fadeIn.RunAsync(this);
    }
}
