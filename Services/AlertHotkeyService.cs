using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.Win32;
using ClassIsland.Core;
using ClassIsland.Core.Abstractions.Services.NotificationProviders;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Models.Notification;
using ClassIsland.Shared;
using EntertainingIsland.Helpers;
using EntertainingIsland.Models;
using Ns = ClassIsland.Shared.Models.Notification;

namespace EntertainingIsland.Services;

/// <summary>
/// 全局热键服务 - 管理警报与安全键。
/// Ctrl+Shift+J 触发警报通知，Ctrl+Shift+K 全局安全键（消警 + 隐藏/显示所有娱乐组件）。
/// </summary>
[NotificationProviderInfo(
    "611367B9-5911-4101-BEF0-4B74AB76A2B1",
    "窗外有老师警报",
    "\uF431",
    "按下热键触发警报通知，提醒学生外面有老师视奸。"
)]
public class AlertHotkeyService : NotificationProviderBase<AlertHotkeySettings>
{
    private const int WM_HOTKEY = 0x0312;
    private const int ALERT_ID = 9001;
    private const int DISMISS_ID = 9002;
    private const int CATCHPHRASE_BASE_ID = 9100;  // 口头禅热键从 9100 开始

    private nint _hwnd;
    private bool _registered;
    private bool _hooked;
    private Settings? _pluginSettings;
    private NotificationRequest? _currentRequest;
    private System.Timers.Timer? _retry;
    private EntertainmentState? _state;
    private CatchphraseStore? _catchphraseStore;

    public AlertHotkeyService(EntertainmentState state, CatchphraseStore catchphraseStore)
    {
        _state = state;
        _catchphraseStore = catchphraseStore;
        if (!OperatingSystem.IsWindows()) return;
        Logger.Info("[ctor] 等待 AppStarted...");
        AppBase.Current.AppStarted += OnInit;
    }

    private void OnInit(object? s, EventArgs e)
    {
        Logger.Info("AppStarted");
        var plugin = IAppHost.GetService<Plugin>();
        _pluginSettings = plugin.Settings;
        Logger.Info($"设置: Alert={Settings.AlertHotkey.DisplayString} Dismiss={_pluginSettings.DismissHotkey.DisplayString}");

        // 监听提醒设置变更（警报热键、启停）
        Settings.PropertyChanged += (_, a) =>
        {
            if (a.PropertyName == nameof(AlertHotkeySettings.EnableTeacherAlert))
                Dispatcher.UIThread.Post(() => { if (Settings.EnableTeacherAlert) TryReg(); else Unreg(); });
        };
        Settings.AlertHotkey.PropertyChanged += (_, _) => Dispatcher.UIThread.Post(() => { Unreg(); TryReg(); });

        // 监听全局安全键变更
        _pluginSettings.DismissHotkey.PropertyChanged += (_, _) => Dispatcher.UIThread.Post(() => { Unreg(); TryReg(); });

        _retry = new System.Timers.Timer(2000);
        _retry.Elapsed += (_, _) => Dispatcher.UIThread.Post(TryReg);
        _retry.AutoReset = true;
        _retry.Start();
        Dispatcher.UIThread.Post(TryReg);
    }

    private void TryReg()
    {
        if (_registered) { _retry?.Stop(); return; }
        if (!Settings.EnableTeacherAlert) return;

        var w = AppBase.Current.MainWindow ?? AppBase.Current.GetRootWindow();
        if (w == null) { Logger.Warn("窗口未就绪"); return; }
        var ph = w.TryGetPlatformHandle();
        if (ph == null) { Logger.Warn("无句柄"); return; }

        _hwnd = ph.Handle;
        Logger.Info($"句柄: 0x{_hwnd:X}");

        if (!_hooked)
        {
            Win32Properties.AddWndProcHookCallback(w, WndProc);
            _hooked = true;
            Logger.Info("钩子已挂");
        }

        var ak = HotkeyHelper.FixKey(Settings.AlertHotkey.Key);
        var av = HotkeyHelper.VkFromKey(ak);
        var am = Settings.AlertHotkey.GetModifiers();
        Logger.Info($"警报 [{Settings.AlertHotkey.DisplayString}] VK=0x{av:X2} -> {(av > 0 && Reg(ALERT_ID, am, av) ? "OK" : "FAIL")}");

        if (_pluginSettings == null) return;
        var dk = HotkeyHelper.FixKey(_pluginSettings.DismissHotkey.Key);
        var dv = HotkeyHelper.VkFromKey(dk);
        var dm = _pluginSettings.DismissHotkey.GetModifiers();
        Logger.Info($"消警 [{_pluginSettings.DismissHotkey.DisplayString}] VK=0x{dv:X2} -> {(dv > 0 && Reg(DISMISS_ID, dm, dv) ? "OK" : "FAIL")}");

        // 注册口头禅热键
        for (int i = 0; i < _pluginSettings.CatchphrasePresets.Count && i < 20; i++)
        {
            var preset = _pluginSettings.CatchphrasePresets[i];
            var pk = HotkeyHelper.FixKey(preset.Hotkey.Key);
            var pv = HotkeyHelper.VkFromKey(pk);
            var pm = preset.Hotkey.GetModifiers();
            int pid = CATCHPHRASE_BASE_ID + i;
            Logger.Info($"口头禅 [{preset.Phrase}] {preset.Hotkey.DisplayString} -> {(pv > 0 && Reg(pid, pm, pv) ? "OK" : "FAIL")}");
        }

        _registered = true;
        _retry?.Stop();
        Logger.Info("===== 就绪 =====");
    }

    private bool Reg(int id, uint mod, uint vk)
    {
        try { return NativeMethods.RegisterHotKey(_hwnd, id, mod, vk); }
        catch (Exception ex) { Logger.Error($"RegisterHotKey: {ex.Message}"); return false; }
    }

    private void Unreg()
    {
        if (!_registered) return;
        if (_hwnd != IntPtr.Zero) {
            NativeMethods.UnregisterHotKey(_hwnd, ALERT_ID);
            NativeMethods.UnregisterHotKey(_hwnd, DISMISS_ID);
            for (int i = 0; i < 20; i++)
                NativeMethods.UnregisterHotKey(_hwnd, CATCHPHRASE_BASE_ID + i);
        }
        _registered = false;
    }

    private IntPtr WndProc(IntPtr h, uint m, IntPtr w, IntPtr l, ref bool handled)
    {
        if (m == WM_HOTKEY)
        {
            int id = w.ToInt32();
            Logger.Info($"WM_HOTKEY id={id}");
            if (id == ALERT_ID) { handled = true; Alert(); }
            else if (id == DISMISS_ID) { handled = true; SecurityKey(); }
            else if (id >= CATCHPHRASE_BASE_ID && id < CATCHPHRASE_BASE_ID + 20)
            {
                handled = true;
                var idx = id - CATCHPHRASE_BASE_ID;
                if (_pluginSettings != null && idx < _pluginSettings.CatchphrasePresets.Count)
                {
                    var phrase = _pluginSettings.CatchphrasePresets[idx].Phrase;
                    _catchphraseStore?.Add(phrase);
                    Logger.Info($"口头禅 +1: \"{phrase}\"");
                }
            }
        }
        return IntPtr.Zero;
    }

    private void SecurityKey()
    {
        // 1. 消警（如果有活动警报）
        Dismiss();
        // 2. 切换娱乐组件显隐
        if (_state != null)
        {
            _state.IsHidden = !_state.IsHidden;
            Logger.Info($"全局安全键: 所有娱乐组件已{(_state.IsHidden ? "隐藏" : "显示")}");
        }
    }

    private void Alert()
    {
        Dismiss();
        var dur = TimeSpan.FromSeconds(Math.Max(3, Settings.AlertDurationSeconds));
        Logger.Info($"发送通知: \"{Settings.AlertMessage}\" {dur.TotalSeconds}s");

        try
        {
            _currentRequest = new NotificationRequest
            {
                MaskContent = NotificationContent.CreateTwoIconsMask(
                    Settings.AlertMessage,
                    rightIcon: "\ue9e4",
                    factory: x =>
                    {
                        x.Duration = dur;
                        x.SpeechContent = Settings.EnableSpeech ? Settings.AlertMessage : "";
                        x.IsSpeechEnabled = Settings.EnableSpeech;
                        if (Settings.EnableEmphasisEffect)
                        {
                            try
                            {
                                // 使用 ClassIsland 当前主题强调色
                                var faTheme = Avalonia.Application.Current?.Styles
                                    .OfType<FluentAvalonia.Styling.FluentAvaloniaTheme>()
                                    .FirstOrDefault();
                                x.Color = faTheme?.CustomAccentColor != null
                                    ? new SolidColorBrush(faTheme.CustomAccentColor.Value)
                                    : Brush.Parse("#FF6B6B");
                            }
                            catch { }
                        }
                    }),
                OverlayContent = NotificationContent.CreateSimpleTextContent(
                    Settings.AlertMessage,
                    factory: x =>
                    {
                        x.Duration = dur;
                        x.IsSpeechEnabled = Settings.EnableSpeech;
                        x.SpeechContent = Settings.EnableSpeech ? Settings.AlertMessage : "";
                    }),
                RequestNotificationSettings = new Ns.NotificationSettings
                {
                    IsSettingsEnabled = true,
                    IsNotificationEnabled = true,
                    IsNotificationEffectEnabled = Settings.EnableEmphasisEffect,
                    IsSpeechEnabled = Settings.EnableSpeech,
                    IsNotificationSoundEnabled = Settings.EnableSound,
                    IsNotificationTopmostEnabled = Settings.EnableTopmost
                }
            };

            _currentRequest.Canceled += (_, _) => _currentRequest = null;
            ShowNotification(_currentRequest);
            Logger.Info("已发送");
        }
        catch (Exception ex) { Logger.Error($"Alert异常: {ex.Message}"); }
    }

    private void Dismiss()
    {
        if (_currentRequest == null) return;
        try { _currentRequest.Cancel(); }
        catch (Exception ex) { Logger.Error($"消警异常: {ex.Message}"); }
        _currentRequest = null;
    }

}
