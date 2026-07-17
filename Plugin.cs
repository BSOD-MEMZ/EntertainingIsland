using ClassIsland.Core;
using ClassIsland.Core.Abstractions;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Extensions.Registry;
using ClassIsland.Shared;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using EntertainingIsland.Models;
using EntertainingIsland.Services;
using EntertainingIsland.Services.Automation;
using EntertainingIsland.Services.Automation.Triggers;
using EntertainingIsland.Views;
using EntertainingIsland.Views.Components;
using EntertainingIsland.Views.NotificationProviders;
using EntertainingIsland.Views.SettingsPages;
using System.IO;
using ClassIsland.Shared.Helpers;

namespace EntertainingIsland;

/// <summary>
/// 插件入口类。必须继承 <see cref="PluginBase"/> 并添加 <see cref="PluginEntrance"/> 特性。
/// </summary>
[PluginEntrance]
public class Plugin : PluginBase
{
    /// <summary>
    /// 插件设置对象。使用 ConfigureFileHelper 方便地读写 JSON 配置。
    /// </summary>
    public Settings Settings { get; set; } = new();

    /// <summary>点名器浮窗引用（防止 GC）</summary>
    private LuckyPickerWindow? _luckyPickerWindow;

    /// <summary>
    /// 插件初始化方法。在插件加载后立即调用。
    /// 在这里完成服务注册、组件注册、设置页面注册等操作。
    /// </summary>
    public override void Initialize(HostBuilderContext context, IServiceCollection services)
    {
        // 0. 初始化日志（必须在最早时机，确保后续所有 Logger.xxx 调用能写入）
        Logger.Initialize(PluginConfigFolder);

        // 1. 加载插件配置
        var configPath = Path.Combine(PluginConfigFolder, "Settings.json");
        Settings = ConfigureFileHelper.LoadConfig<Settings>(configPath);

        // 确保新增的嵌套设置对象不为 null（兼容旧版配置文件）
        Settings.LuckyPicker ??= new();
        Settings.CameraMonitor ??= new();
        Settings.Fortune ??= new();
        Settings.FeatureToggles ??= new();

        // 当设置发生变化时自动保存
        Settings.PropertyChanged += (sender, args) =>
        {
            ConfigureFileHelper.SaveConfig(configPath, Settings);
        };

        // 口头禅预设的深层变更也要保存（预设文字、快捷键变化不会触发 Settings.PropertyChanged）
        HookCatchphrasePresetsSave(configPath);

        // 点名器深层设置变更也要保存
        HookLuckyPickerSave(configPath);

        // 功能开关深层变更也要保存
        HookFeatureTogglesSave(configPath);

        var ft = Settings.FeatureToggles;

        // 2. 注册全局娱乐状态（单例，被组件和服务共享）
        services.AddSingleton<EntertainmentState>();

        // 2b. 注册口头禅存储（单例，共享实例实现实时更新）
        services.AddSingleton(sp => new CatchphraseStore(PluginConfigFolder));

        // 3. 注册 HTTP 通知提供程序（内部 REST 接口，无需在 ClassIsland UI 配置）
        services.AddNotificationProvider<HttpNotificationProvider>();

        // 3. 注册 HTTP 服务器（独立于通知提供程序，先启动监听）
        services.AddHostedService<HttpNotificationServer>();

        // 4. 注册全局热键警报服务（通知提供方，带提醒设置控件）
        if (ft.AlertHotkey)
            services.AddNotificationProvider<AlertHotkeyService, AlertHotkeySettingsControl>();

        // 4b. 注册下课倒计时提醒服务（通知提供方，带提醒设置控件）
        if (ft.ClassEndingReminder)
            services.AddNotificationProvider<ClassEndingReminderService, ClassEndingReminderSettingsControl>();

        // 5. 注册小说阅读器组件（带设置面板）
        if (ft.NovelReader)
            services.AddComponent<NovelReaderComponent, NovelReaderSettingsControl>();

        // 6. 注册口头禅记录组件（带设置面板）
        if (ft.Catchphrase)
            services.AddComponent<CatchphraseComponent, CatchphraseComponentSettingsControl>();

        // 6b. 注册头像课程表组件（带设置面板）
        if (ft.AvatarClassSchedule)
            services.AddComponent<AvatarClassScheduleComponent, AvatarClassScheduleSettingsControl>();

        // 6c. 注册 RSS 新闻组件（带设置面板）
        if (ft.RssNews)
            services.AddComponent<RssComponent, RssSettingsControl>();

        // 6d. 注册体育赛事组件（带设置面板）
        if (ft.Sports)
            services.AddComponent<SportsComponent, SportsSettingsControl>();

        // 6e. 注册摄像头安全检测服务（单例）
        services.AddSingleton<CameraMonitorService>();

        // 6f. 注册摄像头安全指示器组件（带设置面板）
        if (ft.CameraStatus)
            services.AddComponent<CameraStatusComponent, CameraMonitorSettingsControl>();

        // 6g. 注册每日运势服务（单例）
        services.AddSingleton(sp => new FortuneService(Settings.Fortune));

        // 6h. 注册每日运势组件（带设置面板）
        if (ft.Fortune)
            services.AddComponent<FortuneComponent, FortuneComponentSettingsControl>();

        // 6i. 注册点名器服务（单例）
        if (ft.LuckyPicker)
            services.AddSingleton(sp => new LuckyPickerService(Settings.LuckyPicker));

        // 6j. 注册点名器通知提供程序
        if (ft.LuckyPicker && ft.LuckyPickerNotifier)
        {
            services.AddNotificationProvider<LuckyPickerNotifier>();
            // 额外注册为自身类型，以便 TryGetService 能解析（AddNotificationProvider 只注册为 IHostedService）
            services.AddSingleton(sp => sp.GetServices<IHostedService>().OfType<LuckyPickerNotifier>().First());
        }

        // 7. 注册 ClassIsland 自动化行动（可在自动化规则中使用）

        // 全局组件显隐
        if (ft.ToggleVisibility) services.AddAction<ToggleAllVisibilityAction>();
        if (ft.ShowAllComponents) services.AddAction<ShowAllComponentsAction>();
        if (ft.HideAllComponents) services.AddAction<HideAllComponentsAction>();

        // 小说阅读器
        if (ft.NovelActions)
        {
            services.AddAction<NovelPauseAction>();
            services.AddAction<NovelResumeAction>();
            services.AddAction<NovelNextPageAction>();
            services.AddAction<NovelPrevPageAction>();
            services.AddAction<NovelRestartAction>();
        }

        // RSS 新闻
        if (ft.RssActions)
        {
            services.AddAction<RssNextAction>();
            services.AddAction<RssPrevAction>();
        }

        // 口头禅
        if (ft.CatchphraseClearAction)
            services.AddAction<CatchphraseClearAction>();

        // 摄像头监控 Actions
        if (ft.CameraActions)
        {
            services.AddAction<ToggleCameraMonitorAction>();
            services.AddAction<EnableCameraMonitorAction>();
            services.AddAction<DisableCameraMonitorAction>();
        }

        // 摄像头监控 Triggers（"当事件触发时"）
        if (ft.CameraTriggers)
        {
            services.AddTrigger<CameraInUseTrigger>();
            services.AddTrigger<CameraStoppedTrigger>();
        }

        // 8. 注册设置页面
        services.AddSettingsPage<MySettingsPage>();
        if (ft.LuckyPicker)
            services.AddSettingsPage<LuckyPickerSettingsPage>();
        // Fortune 和 CameraMonitor 已注册为组件，不再在侧边栏显示设置
        // services.AddSettingsPage<FortuneSettingsPage>();
        // services.AddSettingsPage<CameraMonitorSettingsPage>();

        // 7. 应用完全启动后：延迟初始化 + 创建点名器浮窗 + 输出欢迎信息
        AppBase.Current.AppStarted += (o, args) =>
        {
            // 7a. 延迟初始化通知提供程序（此时 IAppHost.Host 已就绪）
            try
            {
                var provider = IAppHost.TryGetService<HttpNotificationProvider>();
                provider?.SafeInitialize();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[EntertainingIsland] 通知提供程序初始化失败: {ex.Message}");
            }

            // 7b. 创建点名器屏幕浮窗
            try
            {
                if (ft.LuckyPicker && Settings.LuckyPicker.IsEnabled)
                {
                    var luckySettings = Settings.LuckyPicker;
                    var service = IAppHost.TryGetService<LuckyPickerService>();
                    if (service == null) return;
                    var notifier = IAppHost.TryGetService<LuckyPickerNotifier>();
                    var window = new LuckyPickerWindow();
                    window.Initialize(luckySettings, service, notifier);

                    // 设为主窗口的子窗口（防止被主窗口遮挡）
                    var mainWindow = AppBase.Current.MainWindow ?? AppBase.Current.GetRootWindow();
                    if (mainWindow != null)
                        window.Show(mainWindow);
                    else
                        window.Show();

                    _luckyPickerWindow = window; // 防止 GC
                    Console.WriteLine("[EntertainingIsland] 点名器浮窗已创建");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[EntertainingIsland] 点名器浮窗创建失败: {ex.Message}");
            }

            // 7c. 初始化摄像头监控服务
            try
            {
                var cameraService = IAppHost.TryGetService<CameraMonitorService>();
                cameraService?.Initialize(Settings.CameraMonitor);
                Console.WriteLine($"[EntertainingIsland] 摄像头监控已启动 (启用: {Settings.CameraMonitor.EnableCameraMonitor})");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[EntertainingIsland] 摄像头监控初始化失败: {ex.Message}");
            }

            // 7d. 输出欢迎信息
            if (Settings.ShowWelcomeMessage)
            {
                Console.WriteLine($"[EntertainingIsland] 欢迎使用 {Info.Manifest.Name}！");
                Console.WriteLine($"  ID: {Info.Manifest.Id}");
                Console.WriteLine($"  版本: {Info.Manifest.Version}");
            }

            // 7e. 首次启动显示 OOBE 欢迎向导
            if (!Settings.HasSeenWelcome)
            {
                try
                {
                    var parent = AppBase.Current.MainWindow ?? AppBase.Current.GetRootWindow();
                    var welcomeWindow = new WelcomeWindow();
                    welcomeWindow.ShowDialog(parent);
                    Console.WriteLine("[EntertainingIsland] 首次启动，已显示欢迎向导");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[EntertainingIsland] 欢迎向导启动失败: {ex.Message}");
                    Settings.HasSeenWelcome = true;
                }
            }
        };
    }

    private void HookCatchphrasePresetsSave(string configPath)
    {
        // 监听预设集合增删
        Settings.CatchphrasePresets.CollectionChanged += (_, _) =>
        {
            ConfigureFileHelper.SaveConfig(configPath, Settings);
            // 重新挂接新条目的 PropertyChanged
            RehookPresetItems(configPath);
        };

        // 监听已有条目的属性变化
        RehookPresetItems(configPath);
    }

    private void RehookPresetItems(string configPath)
    {
        foreach (var preset in Settings.CatchphrasePresets)
        {
            preset.PropertyChanged -= OnPresetChanged;
            preset.PropertyChanged += OnPresetChanged;
            preset.Hotkey.PropertyChanged -= OnPresetChanged;
            preset.Hotkey.PropertyChanged += OnPresetChanged;
        }
        return;

        void OnPresetChanged(object? s, System.ComponentModel.PropertyChangedEventArgs e)
        {
            ConfigureFileHelper.SaveConfig(configPath, Settings);
        }
    }

    private void HookLuckyPickerSave(string configPath)
    {
        Settings.LuckyPicker.PropertyChanged += (_, _) =>
        {
            ConfigureFileHelper.SaveConfig(configPath, Settings);
        };
    }

    private void HookFeatureTogglesSave(string configPath)
    {
        Settings.FeatureToggles.PropertyChanged += (_, _) =>
        {
            ConfigureFileHelper.SaveConfig(configPath, Settings);
        };
    }

    /// <summary>公开的静态方法，用于在任何地方触发 OOBE 欢迎向导（例如设置页按钮）</summary>
    public static void ShowWelcomeWizard()
    {
        var parent = AppBase.Current.MainWindow ?? AppBase.Current.GetRootWindow();
        var window = new WelcomeWindow();
        window.ShowDialog(parent);
    }
}
