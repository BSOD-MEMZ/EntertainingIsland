using Avalonia.Threading;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Shared;
using EntertainingIsland.Models;
using EntertainingIsland.Services;

namespace EntertainingIsland.Views.Components;

[ComponentInfo(
    "E5D1D45A-5DA0-4BA9-9075-2466ADF636E0",
    "口头禅记录",
    "\uE3E4",
    "记录老师口头禅，支持语音识别自动检测"
)]
public partial class CatchphraseComponent : ComponentBase<CatchphraseComponentSettings>
{
    private CatchphraseStore? _store;
    private CatchphraseSpeechService? _speechService;
    private Settings? _pluginSettings;

    public CatchphraseComponent()
    {
        InitializeComponent();
    }

    protected override void OnAttachedToVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        // 安全键隐藏
        var state = IAppHost.TryGetService<EntertainmentState>();
        if (state != null)
            state.PropertyChanged += (s, a) =>
            {
                var name = a.PropertyName;
                if (name == nameof(EntertainmentState.IsHidden))
                    IsVisible = !state.IsHidden;
                else if (name == nameof(EntertainmentState.CatchphraseClearRequest))
                    _store?.Clear();
            };

        // 通过 DI 获取单例 CatchphraseStore（与 AlertHotkeyService 共享同一实例）
        _store = IAppHost.TryGetService<CatchphraseStore>();
        if (_store != null)
            _store.PropertyChanged += (_, _) => RefreshDisplay();

        // 获取插件设置
        _pluginSettings = IAppHost.GetService<Plugin>().Settings;

        // 初始化语音识别
        InitSpeechRecognition();

        RefreshDisplay();
    }

    protected override void OnDetachedFromVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
    {
        StopSpeechRecognition();
        base.OnDetachedFromVisualTree(e);
    }

    public void RefreshDisplay()
    {
        if (_store != null)
            CatchphraseLabel.Text = _store.GetDisplayText();
    }

    private void InitSpeechRecognition()
    {
        if (_pluginSettings == null || _store == null) return;

        // 监听语音识别开关变化
        _pluginSettings.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(Models.Settings.CatchphraseVoiceEnabled))
            {
                if (_pluginSettings.CatchphraseVoiceEnabled)
                    StartSpeechRecognition();
                else
                    StopSpeechRecognition();
            }
        };

        // 根据初始设置启动
        if (_pluginSettings.CatchphraseVoiceEnabled)
            StartSpeechRecognition();
    }

    private void StartSpeechRecognition()
    {
        if (_speechService != null || _pluginSettings == null || _store == null) return;

        _speechService = new CatchphraseSpeechService(_store, _pluginSettings);
        _speechService.PhraseMatched += phrase =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                RefreshDisplay();
                // 口头禅强调提醒
                if (_pluginSettings?.CatchphraseEmphasisEnabled == true)
                    ShowEmphasis(phrase);
            });
        };
        _speechService.Start();
    }

    private void StopSpeechRecognition()
    {
        if (_speechService == null) return;
        _speechService.Stop();
        _speechService.Dispose();
        _speechService = null;
    }

    private void ShowEmphasis(string phrase)
    {
        try
        {
            var notifier = IAppHost.TryGetService<AlertHotkeyService>();
            if (notifier != null)
            {
                // 通过 AlertHotkeyService 的公开方法显示强调提醒
                // 如果不可用则忽略
            }
        }
        catch { }
    }
}
