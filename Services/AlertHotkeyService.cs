using System;
using System.Collections.Generic;
using System.Runtime.Versioning;
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
/// 跨平台全局热键服务 - 管理警报与安全键。
/// Windows 用 RegisterHotKey，Linux 用 X11 XGrabKey。
/// </summary>
[NotificationProviderInfo(
    "49D83EC3-E84A-4072-97AB-C101C121DAA1",
    "窗外有老师警报",
    "\uF431",
    "按下热键触发警报通知，提醒学生外面有老师视奸。"
)]
public class AlertHotkeyService : NotificationProviderBase<AlertHotkeySettings>
{
    private const int WM_HOTKEY = 0x0312;
    private const int ALERT_ID = 9001;
    private const int DISMISS_ID = 9002;
    private const int CATCHPHRASE_BASE_ID = 9100;

    // Windows
    private nint _hwnd;
    private bool _hooked;

    // 通用
    private bool _registered;
    private Settings? _pluginSettings;
    private NotificationRequest? _currentRequest;
    private System.Timers.Timer? _retry;
    private EntertainmentState? _state;
    private CatchphraseStore? _catchphraseStore;

    // Linux X11
    private IntPtr _x11Display;
    private DispatcherTimer? _x11Timer;
    private Dictionary<(int keycode, uint mod), int>? _x11Map;

    public AlertHotkeyService(EntertainmentState state, CatchphraseStore catchphraseStore)
    {
        _state = state;
        _catchphraseStore = catchphraseStore;
        Logger.Info("[AlertHotkey] 等待 AppStarted...");
        AppBase.Current.AppStarted += OnInit;
    }

    private void OnInit(object? s, EventArgs e)
    {
        Logger.Info("[AlertHotkey] AppStarted");
        var plugin = IAppHost.GetService<Plugin>();
        _pluginSettings = plugin.Settings;
        Logger.Info($"设置: Alert={Settings.AlertHotkey.DisplayString} Dismiss={_pluginSettings.DismissHotkey.DisplayString}");

        Settings.PropertyChanged += (_, a) =>
        {
            if (a.PropertyName == nameof(AlertHotkeySettings.EnableTeacherAlert))
                Dispatcher.UIThread.Post(() => { if (Settings.EnableTeacherAlert) TryReg(); else Unreg(); });
        };
        Settings.AlertHotkey.PropertyChanged += (_, _) => Dispatcher.UIThread.Post(() => { Unreg(); TryReg(); });

        _pluginSettings.DismissHotkey.PropertyChanged += (_, _) => Dispatcher.UIThread.Post(() => { Unreg(); TryReg(); });

        _retry = new System.Timers.Timer(2000);
        _retry.Elapsed += (_, _) => Dispatcher.UIThread.Post(TryReg);
        _retry.AutoReset = true;
        _retry.Start();
        Dispatcher.UIThread.Post(TryReg);
    }

    // ==================== 注册调度 ====================

    private void TryReg()
    {
        if (_registered) { _retry?.Stop(); return; }
        if (!Settings.EnableTeacherAlert) return;

        if (OperatingSystem.IsWindows())
            TryRegWindows();
        else if (OperatingSystem.IsLinux())
            TryRegLinux();
    }

    // ==================== Windows 实现 ====================

    [SupportedOSPlatform("windows")]
    private void TryRegWindows()
    {
        var w = AppBase.Current.MainWindow ?? AppBase.Current.GetRootWindow();
        if (w == null) { Logger.Warn("[AlertHotkey] 窗口未就绪"); return; }
        var ph = w.TryGetPlatformHandle();
        if (ph == null) { Logger.Warn("[AlertHotkey] 无句柄"); return; }

        _hwnd = ph.Handle;
        Logger.Info($"[AlertHotkey] 句柄: 0x{_hwnd:X}");

        if (!_hooked)
        {
            Win32Properties.AddWndProcHookCallback(w, WndProc);
            _hooked = true;
            Logger.Info("[AlertHotkey] 钩子已挂");
        }

        var ak = HotkeyHelper.FixKey(Settings.AlertHotkey.Key);
        var av = HotkeyHelper.VkFromKey(ak);
        var am = Settings.AlertHotkey.GetModifiers();
        Logger.Info($"警报 [{Settings.AlertHotkey.DisplayString}] VK=0x{av:X2} -> {(av > 0 && RegWin(ALERT_ID, am, av) ? "OK" : "FAIL")}");

        if (_pluginSettings == null) return;
        var dk = HotkeyHelper.FixKey(_pluginSettings.DismissHotkey.Key);
        var dv = HotkeyHelper.VkFromKey(dk);
        var dm = _pluginSettings.DismissHotkey.GetModifiers();
        Logger.Info($"消警 [{_pluginSettings.DismissHotkey.DisplayString}] VK=0x{dv:X2} -> {(dv > 0 && RegWin(DISMISS_ID, dm, dv) ? "OK" : "FAIL")}");

        for (int i = 0; i < _pluginSettings.CatchphrasePresets.Count && i < 20; i++)
        {
            var preset = _pluginSettings.CatchphrasePresets[i];
            var pk = HotkeyHelper.FixKey(preset.Hotkey.Key);
            var pv = HotkeyHelper.VkFromKey(pk);
            var pm = preset.Hotkey.GetModifiers();
            int pid = CATCHPHRASE_BASE_ID + i;
            Logger.Info($"口头禅 [{preset.Phrase}] {preset.Hotkey.DisplayString} -> {(pv > 0 && RegWin(pid, pm, pv) ? "OK" : "FAIL")}");
        }

        _registered = true;
        _retry?.Stop();
        Logger.Info("[AlertHotkey] ===== Windows 就绪 =====");
    }

    [SupportedOSPlatform("windows")]
    private bool RegWin(int id, uint mod, uint vk)
    {
        try { return NativeMethods.RegisterHotKey(_hwnd, id, mod, vk); }
        catch (Exception ex) { Logger.Error($"RegisterHotKey: {ex.Message}"); return false; }
    }

    [SupportedOSPlatform("windows")]
    private void UnregWindows()
    {
        if (_hwnd != IntPtr.Zero)
        {
            NativeMethods.UnregisterHotKey(_hwnd, ALERT_ID);
            NativeMethods.UnregisterHotKey(_hwnd, DISMISS_ID);
            for (int i = 0; i < 20; i++)
                NativeMethods.UnregisterHotKey(_hwnd, CATCHPHRASE_BASE_ID + i);
        }
    }

    private IntPtr WndProc(IntPtr h, uint m, IntPtr w, IntPtr l, ref bool handled)
    {
        if (m == WM_HOTKEY)
        {
            int id = w.ToInt32();
            Logger.Info($"WM_HOTKEY id={id}");
            HandleHotkey(id);
            handled = true;
        }
        return IntPtr.Zero;
    }

    // ==================== Linux X11 实现 ====================

    [SupportedOSPlatform("linux")]
    private void TryRegLinux()
    {
        try
        {
            // 必须在任何 X11 调用之前安装安全错误处理器，防止默认 exit(1) 崩溃
            NativeMethods.InstallX11ErrorHandler();

            _x11Display = NativeMethods.XOpenDisplay(null);
            if (_x11Display == IntPtr.Zero)
            {
                Logger.Warn("[AlertHotkey] 无法打开 X11 Display，热键不可用");
                return;
            }

            var root = NativeMethods.XDefaultRootWindow(_x11Display);

            // 注册警报热键
            {
                var ak = HotkeyHelper.FixKey(Settings.AlertHotkey.Key);
                var kc = HotkeyHelper.X11KeycodeFromKey(_x11Display, ak);
                var mod = Settings.AlertHotkey.GetX11Modifiers();
                var ok = kc > 0 && NativeMethods.XGrabKey(_x11Display, kc, mod, root,
                    true, NativeMethods.X11GrabModeAsync, NativeMethods.X11GrabModeAsync) == 0;
                Logger.Info($"警报 [{Settings.AlertHotkey.DisplayString}] X11 -> {(ok ? "OK" : "FAIL")}");
            }

            if (_pluginSettings != null)
            {
                // 注册消警热键
                {
                    var dk = HotkeyHelper.FixKey(_pluginSettings.DismissHotkey.Key);
                    var kc = HotkeyHelper.X11KeycodeFromKey(_x11Display, dk);
                    var mod = _pluginSettings.DismissHotkey.GetX11Modifiers();
                    var ok = kc > 0 && NativeMethods.XGrabKey(_x11Display, kc, mod, root,
                        true, NativeMethods.X11GrabModeAsync, NativeMethods.X11GrabModeAsync) == 0;
                    Logger.Info($"消警 [{_pluginSettings.DismissHotkey.DisplayString}] X11 -> {(ok ? "OK" : "FAIL")}");
                }

                // 注册口头禅热键
                for (int i = 0; i < _pluginSettings.CatchphrasePresets.Count && i < 20; i++)
                {
                    var preset = _pluginSettings.CatchphrasePresets[i];
                    var pk = HotkeyHelper.FixKey(preset.Hotkey.Key);
                    var kc = HotkeyHelper.X11KeycodeFromKey(_x11Display, pk);
                    var mod = preset.Hotkey.GetX11Modifiers();
                    var ok = kc > 0 && NativeMethods.XGrabKey(_x11Display, kc, mod, root,
                        true, NativeMethods.X11GrabModeAsync, NativeMethods.X11GrabModeAsync) == 0;
                    Logger.Info($"口头禅 [{preset.Phrase}] {preset.Hotkey.DisplayString} X11 -> {(ok ? "OK" : "FAIL")}");
                }
            }

            NativeMethods.XFlush(_x11Display);
            _registered = true;
            _retry?.Stop();

            // 使用 DispatcherTimer 在主 UI 线程轮询 X11 事件（避免多线程 X11 崩溃）
            _x11Timer = new DispatcherTimer(TimeSpan.FromMilliseconds(80), DispatcherPriority.Background, OnX11Tick);
            _x11Timer.Start();
            Logger.Info("[AlertHotkey] ===== Linux X11 就绪 =====");
        }
        catch (Exception ex)
        {
            Logger.Warn($"[AlertHotkey] Linux 热键注册失败: {ex.Message}");
            if (_x11Display != IntPtr.Zero)
            {
                NativeMethods.XCloseDisplay(_x11Display);
                _x11Display = IntPtr.Zero;
            }
        }
    }

    /// <summary>DispatcherTimer 回调：在主 UI 线程轮询 X11 事件</summary>
    private void OnX11Tick(object? sender, EventArgs e)
    {
        // 延迟初始化映射表（需要 _x11Display 就绪）
        if (_x11Map == null && _x11Display != IntPtr.Zero)
        {
            _x11Map = new Dictionary<(int, uint), int>();
            void Add(string key, HotkeyConfig cfg, int id)
            {
                var kc = HotkeyHelper.X11KeycodeFromKey(_x11Display, HotkeyHelper.FixKey(key));
                var mod = cfg.GetX11Modifiers();
                if (kc > 0) _x11Map[(kc, mod)] = id;
            }
            Add(Settings.AlertHotkey.Key, Settings.AlertHotkey, ALERT_ID);
            if (_pluginSettings != null)
            {
                Add(_pluginSettings.DismissHotkey.Key, _pluginSettings.DismissHotkey, DISMISS_ID);
                for (int i = 0; i < _pluginSettings.CatchphrasePresets.Count && i < 20; i++)
                    Add(_pluginSettings.CatchphrasePresets[i].Hotkey.Key, _pluginSettings.CatchphrasePresets[i].Hotkey, CATCHPHRASE_BASE_ID + i);
            }
        }

        if (_x11Display == IntPtr.Zero || _x11Map == null) return;

        try
        {
            while (NativeMethods.XPending(_x11Display) > 0)
            {
                _ = NativeMethods.XNextEvent(_x11Display, out var ev);
                if (ev.type == NativeMethods.X11KeyPress)
                {
                    var ke = ev.key;
                    var clean = ke.state & ~(NativeMethods.X11LockMask | NativeMethods.X11Mod2Mask | NativeMethods.X11Mod3Mask | NativeMethods.X11Mod5Mask);
                    if (_x11Map.TryGetValue((ke.keycode, clean), out var id))
                    {
                        Logger.Info($"[AlertHotkey] X11 KeyPress keycode={ke.keycode} id={id}");
                        HandleHotkey(id);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Warn($"[AlertHotkey] X11 Tick 异常: {ex.Message}");
        }
    }

    [SupportedOSPlatform("linux")]
    private void UnregLinux()
    {
        _x11Timer?.Stop();
        _x11Timer = null;
        _x11Map = null;

        if (_x11Display != IntPtr.Zero)
        {
            try
            {
                var root = NativeMethods.XDefaultRootWindow(_x11Display);
                void Ungrab(string key, HotkeyConfig cfg)
                {
                    var kc = HotkeyHelper.X11KeycodeFromKey(_x11Display, HotkeyHelper.FixKey(key));
                    var mod = cfg.GetX11Modifiers();
                    if (kc > 0) NativeMethods.XUngrabKey(_x11Display, kc, mod, root);
                }
                Ungrab(Settings.AlertHotkey.Key, Settings.AlertHotkey);
                if (_pluginSettings != null)
                {
                    Ungrab(_pluginSettings.DismissHotkey.Key, _pluginSettings.DismissHotkey);
                    for (int i = 0; i < _pluginSettings.CatchphrasePresets.Count && i < 20; i++)
                        Ungrab(_pluginSettings.CatchphrasePresets[i].Hotkey.Key, _pluginSettings.CatchphrasePresets[i].Hotkey);
                }
                NativeMethods.XFlush(_x11Display);
                NativeMethods.XCloseDisplay(_x11Display);
            }
            catch { }
            _x11Display = IntPtr.Zero;
        }
    }

    // ==================== 通用：注销 / 事件处理 ====================

    private void Unreg()
    {
        if (!_registered) return;

        if (OperatingSystem.IsWindows())
        {
            UnregWindows();
        }
        else if (OperatingSystem.IsLinux())
        {
            UnregLinux();
        }

        _registered = false;
    }

    /// <summary>统一的热键事件处理</summary>
    private void HandleHotkey(int id)
    {
        if (id == ALERT_ID) { Alert(); }
        else if (id == DISMISS_ID) { SecurityKey(); }
        else if (id >= CATCHPHRASE_BASE_ID && id < CATCHPHRASE_BASE_ID + 20)
        {
            var idx = id - CATCHPHRASE_BASE_ID;
            if (_pluginSettings != null && idx < _pluginSettings.CatchphrasePresets.Count)
            {
                var phrase = _pluginSettings.CatchphrasePresets[idx].Phrase;
                _catchphraseStore?.Add(phrase);
                Logger.Info($"口头禅 +1: \"{phrase}\"");

                if (_pluginSettings.CatchphraseEmphasisEnabled)
                    ShowCatchphraseEmphasis(phrase);
            }
        }
    }

    // ==================== 通知 / 业务逻辑（平台无关） ====================

    private void SecurityKey()
    {
        Dismiss();
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

    private void ShowCatchphraseEmphasis(string phrase)
    {
        try
        {
            var content = NotificationContent.CreateTwoIconsMask(
                $"{phrase}",
                rightIcon: "\uE3E4",
                factory: x =>
                {
                    x.Duration = TimeSpan.FromSeconds(1);
                });

            var request = new NotificationRequest
            {
                MaskContent = content,
                RequestNotificationSettings = new Ns.NotificationSettings
                {
                    IsSettingsEnabled = false,
                    IsNotificationEnabled = true,
                    IsNotificationEffectEnabled = true,
                    IsNotificationTopmostEnabled = true
                }
            };

            ShowNotification(request);
        }
        catch (Exception ex)
        {
            Logger.Error($"[口头禅提醒] 发送失败: {ex.Message}");
        }
    }
}
