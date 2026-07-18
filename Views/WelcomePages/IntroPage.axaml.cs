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
    private static bool _hasPlayed;

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

        if (_hasPlayed)
        {
            try
            {
                Classes.Add("anim-zoom");
                if (AppIcon != null)
                {
                    AppIcon.Opacity = 1;
                    var scale = AppIcon.RenderTransform as ScaleTransform;
                    if (scale != null) { scale.ScaleX = 1; scale.ScaleY = 1; }
                }
                if (LettersPanel != null)
                {
                    foreach (var t in LettersPanel.Children.OfType<TextBlock>())
                    {
                        t.Opacity = 1;
                        var tt = t.RenderTransform as TranslateTransform;
                        if (tt != null) tt.Y = 0;
                    }
                }
                if (SubtitleText != null) SubtitleText.Opacity = 1;
                return;
            }
            catch { /* fall through to replay */ }
        }

        _hasPlayed = true;
        _ = PlayAnimationAsync();
    }

    private async Task PlayAnimationAsync()
    {
        Classes.Add("anim-zoom");
        var letters = LettersPanel.Children.OfType<TextBlock>().ToList();

        // 音频已在 SoundChoicePage 中预热播放，此处不再重复调用
        // 等待音频到达卡点节拍后再启动画面动画
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

    /// <summary>外部可用：异步播放 WAV 文件（audio 设备预热）</summary>
    public static void PlaySoundStatic(string filePath)
    {
        PlaySound(filePath, IntPtr.Zero, SND_ASYNC | SND_FILENAME);
    }

    /// <summary>外部可用：停止当前播放的声音</summary>
    public static void StopSoundStatic()
    {
        PlaySound(null, IntPtr.Zero, 0);
    }

    private void ButtonNext_OnClick(object? sender, RoutedEventArgs e) => WelcomeWindow.FromControl(this)?.NavigateForward();
}