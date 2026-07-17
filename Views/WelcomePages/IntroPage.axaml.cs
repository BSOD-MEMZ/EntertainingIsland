using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
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

public partial class IntroPage : UserControl, IWelcomePage
{
    public WelcomeViewModel ViewModel { get; set; } = null!;

    public IntroPage() { InitializeComponent(); }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        try
        {
            var plugin = IAppHost.TryGetService<Plugin>();
            if (plugin != null) { var p = Path.Combine(plugin.Info.PluginFolderPath, "icon.png"); if (File.Exists(p)) AppIcon.Source = new Bitmap(p); }
        }
        catch { }
        _ = PlayAnimationAsync();
    }

    private async Task PlayAnimationAsync()
    {
        Classes.Add("anim-zoom");
        var letters = LettersPanel.Children.OfType<TextBlock>().ToList();

        // Start audio IMMEDIATELY via winmm (lowest latency)
        try
        {
            var plugin = IAppHost.TryGetService<Plugin>();
            if (plugin != null)
            {
                var audioPath = Path.Combine(plugin.Info.PluginFolderPath, "intro.wav");
                if (File.Exists(audioPath))
                    PlaySound(audioPath, IntPtr.Zero, SND_ASYNC | SND_FILENAME);
            }
        }
        catch { }

        // Wait for audio to hit 0.3s mark
        await Task.Delay(300);

        // Phase 1: Icon scale+fade in
        var iconAnim = new Animation { FillMode = FillMode.Both, Duration = TimeSpan.FromMilliseconds(800), Easing = new CubicEaseOut() };
        iconAnim.Children.Add(new KeyFrame { Cue = new Cue(0.0) }); iconAnim.Children[0].Setters.Add(new Setter(OpacityProperty, 0.0));
        iconAnim.Children[0].Setters.Add(new Setter(ScaleTransform.ScaleXProperty, 0.5));
        iconAnim.Children[0].Setters.Add(new Setter(ScaleTransform.ScaleYProperty, 0.5));
        iconAnim.Children.Add(new KeyFrame { Cue = new Cue(1.0) }); iconAnim.Children[1].Setters.Add(new Setter(OpacityProperty, 1.0));
        iconAnim.Children[1].Setters.Add(new Setter(ScaleTransform.ScaleXProperty, 1.0));
        iconAnim.Children[1].Setters.Add(new Setter(ScaleTransform.ScaleYProperty, 1.0));
        await iconAnim.RunAsync(AppIcon);

        // Phase 2: Letters sequential entrance
        var count = letters.Count;
        var duration = 2600.0;
        for (int i = 0; i < count; i++)
        {
            var delay = Math.Sin((i + 2.0) / (count + 2.0) * (Math.PI / 2)) * duration / count;
            var anim = new Animation { FillMode = FillMode.Both, Duration = TimeSpan.FromMilliseconds(400), Easing = new CubicEaseOut() };
            anim.Children.Add(new KeyFrame { Cue = new Cue(0.0) });
            anim.Children[0].Setters.Add(new Setter(OpacityProperty, 0.0));
            anim.Children[0].Setters.Add(new Setter(TranslateTransform.YProperty, 40.0));
            anim.Children.Add(new KeyFrame { Cue = new Cue(1.0) });
            anim.Children[1].Setters.Add(new Setter(OpacityProperty, 1.0));
            anim.Children[1].Setters.Add(new Setter(TranslateTransform.YProperty, 0.0));
            _ = anim.RunAsync(letters[i]);
            await Task.Delay(TimeSpan.FromMilliseconds(delay));
        }

        await Task.Delay(300);

        // Phase 3: Subtitle fade in
        var sub = new Animation { FillMode = FillMode.Both, Duration = TimeSpan.FromMilliseconds(500) };
        sub.Children.Add(new KeyFrame { Cue = new Cue(0.0) }); sub.Children[0].Setters.Add(new Setter(OpacityProperty, 0.0));
        sub.Children.Add(new KeyFrame { Cue = new Cue(1.0) }); sub.Children[1].Setters.Add(new Setter(OpacityProperty, 1.0));
        await sub.RunAsync(SubtitleText);

        // Phase 4: Single accelerating collapse, 5.0s start, 400ms
        await Task.Delay(1700);
        await CollapseLetters(letters, 0.0, 1.0, 400, new ExponentialEaseIn());
    }

    private static async Task CollapseLetters(System.Collections.Generic.IList<TextBlock> letters, double fromPct, double toPct, double ms, Easing ease)
    {
        const double fullWidth = 32.0;
        var tasks = letters.Select(l =>
        {
            var fromW = fullWidth * (1.0 - fromPct);
            var toW = fullWidth * (1.0 - toPct);
            var anim = new Animation { FillMode = FillMode.Both, Duration = TimeSpan.FromMilliseconds(ms), Easing = ease };
            anim.Children.Add(new KeyFrame { Cue = new Cue(0.0) }); anim.Children[0].Setters.Add(new Setter(TextBlock.MinWidthProperty, fromW));
            anim.Children.Add(new KeyFrame { Cue = new Cue(1.0) }); anim.Children[1].Setters.Add(new Setter(TextBlock.MinWidthProperty, toW));
            return anim.RunAsync(l);
        }).ToList();
        await Task.WhenAll(tasks);
    }

    [DllImport("winmm.dll", CharSet = CharSet.Auto)]
    private static extern bool PlaySound(string pszSound, IntPtr hmod, uint fdwSound);
    private const uint SND_ASYNC = 0x0001;
    private const uint SND_FILENAME = 0x00020000;

    private void ButtonNext_OnClick(object? sender, RoutedEventArgs e) => WelcomeWindow.FromControl(this)?.NavigateForward();
}