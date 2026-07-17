using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using ClassIsland.Shared;
using EntertainingIsland.ViewModels;

namespace EntertainingIsland.Views.WelcomePages;

public partial class FeaturesPage : UserControl, IWelcomePage
{
    public WelcomeViewModel ViewModel { get; set; } = null!;

    public FeaturesPage()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        _ = PlayEntranceAsync();
    }

    private async Task PlayEntranceAsync()
    {
        try
        {
            var plugin = IAppHost.TryGetService<Plugin>();
            if (plugin != null)
            {
                var shot7 = Path.Combine(plugin.Info.PluginFolderPath, "AeroShot7.png");
                if (File.Exists(shot7)) ScreenshotAvatar.Source = new Bitmap(shot7);
                var shot8 = Path.Combine(plugin.Info.PluginFolderPath, "AeroShot8.png");
                if (File.Exists(shot8)) ScreenshotCountdown.Source = new Bitmap(shot8);
            }
        }
        catch { }

        var highlightAnim = new Animation { FillMode = FillMode.Both, Duration = TimeSpan.FromMilliseconds(500), Easing = new CubicEaseOut() };
        highlightAnim.Children.Add(new KeyFrame { Cue = new Cue(0.0) }); highlightAnim.Children[0].Setters.Add(new Setter(OpacityProperty, 0.0));
        highlightAnim.Children[0].Setters.Add(new Setter(TranslateTransform.YProperty, 30.0));
        highlightAnim.Children.Add(new KeyFrame { Cue = new Cue(1.0) }); highlightAnim.Children[1].Setters.Add(new Setter(OpacityProperty, 1.0));
        highlightAnim.Children[1].Setters.Add(new Setter(TranslateTransform.YProperty, 0.0));
        _ = highlightAnim.RunAsync(HighlightAvatar);
        _ = highlightAnim.RunAsync(HighlightCountdown);

        var cards = CardsPanel.Children.OfType<Border>().ToList();
        for (int i = 0; i < cards.Count; i++)
        {
            var anim = new Animation { FillMode = FillMode.Both, Duration = TimeSpan.FromMilliseconds(350), Easing = new CubicEaseOut() };
            anim.Children.Add(new KeyFrame { Cue = new Cue(0.0) });
            anim.Children[0].Setters.Add(new Setter(OpacityProperty, 0.0));
            anim.Children[0].Setters.Add(new Setter(TranslateTransform.XProperty, 40.0));
            anim.Children.Add(new KeyFrame { Cue = new Cue(1.0) });
            anim.Children[1].Setters.Add(new Setter(OpacityProperty, 1.0));
            anim.Children[1].Setters.Add(new Setter(TranslateTransform.XProperty, 0.0));
            _ = anim.RunAsync(cards[i]);
            await Task.Delay(55);
        }
    }

    private void ButtonNext_OnClick(object? sender, RoutedEventArgs e) => WelcomeWindow.FromControl(this)?.NavigateForward();
    private void ButtonBack_OnClick(object? sender, RoutedEventArgs e) => WelcomeWindow.FromControl(this)?.NavigateBack();

    private void LinkBilibili_Click(object? sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo("https://www.bilibili.com/video/BV15KEt6mEFP/") { UseShellExecute = true });
    }

    private void LinkAuthor_Click(object? sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo("https://xxtsoft.top") { UseShellExecute = true });
    }
}
