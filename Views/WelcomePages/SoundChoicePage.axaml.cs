using System;
using System.IO;
using Avalonia.Controls;
using Avalonia.Interactivity;
using ClassIsland.Shared;
using EntertainingIsland.ViewModels;

namespace EntertainingIsland.Views.WelcomePages;

public partial class SoundChoicePage : UserControl, IWelcomePage
{
    public WelcomeViewModel ViewModel { get; set; } = null!;

    public SoundChoicePage()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        // 页面一加载就播放 12s 无声 WAV，保持音频设备唤醒
        try
        {
            var plugin = IAppHost.TryGetService<Plugin>();
            if (plugin != null)
            {
                var path = Path.Combine(plugin.Info.PluginFolderPath, "musei.wav");
                if (File.Exists(path))
                    IntroPage.PlaySoundStatic(path);
            }
        }
        catch { }
    }

    private void ButtonMute_OnClick(object? sender, RoutedEventArgs e)
    {
        // 停止无声预热
        IntroPage.StopSoundStatic();
        Plugin.OobeSoundEnabled = false;
        WelcomeWindow.FromControl(this)?.NavigateForward();
    }

    private void ButtonSound_OnClick(object? sender, RoutedEventArgs e)
    {
        Plugin.OobeSoundEnabled = true;

        // 停止无声预热，切入正式 intro（设备已持续唤醒，零延迟）
        IntroPage.StopSoundStatic();

        try
        {
            var plugin = IAppHost.TryGetService<Plugin>();
            if (plugin != null)
            {
                var path = Path.Combine(plugin.Info.PluginFolderPath, "intro.wav");
                if (File.Exists(path))
                    IntroPage.PlaySoundStatic(path);
            }
        }
        catch { }

        WelcomeWindow.FromControl(this)?.NavigateForward();
    }
}
