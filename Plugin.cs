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
        // 1. 加载插件配置
        var configPath = Path.Combine(PluginConfigFolder, "Settings.json");
        Settings = ConfigureFileHelper.LoadConfig<Settings>(configPath);

        // 确保新增的嵌套设置对象不为 null（兼容旧版配置文件）
        Settings.LuckyPicker ??= new();
        Settings.CameraMonitor ??= new();

        // 当设置发生变化时自动保存
        Settings.PropertyChanged += (sender, args) =>
        {
            ConfigureFileHelper.SaveConfig(configPath, Settings);
        };

        // 口头禅预设的深层变更也要保存（预设文字、快捷键变化不会触发 Settings.PropertyChanged）
        HookCatchphrasePresetsSave(configPath);

        // 点名器深层设置变更也要保存
        HookLuckyPickerSave(configPath);

        // 2. 注册全局娱乐状态（单例，被组件和服务共享）
        services.AddSingleton<EntertainmentState>();

        // 2b. 注册口头禅存储（单例，共享实例实现实时更新）
        services.AddSingleton(sp => new CatchphraseStore(PluginConfigFolder));

        // 3. 注册 HTTP 通知提供程序
        services.AddNotificationProvider<HttpNotificationProvider>();

        // 3. 注册 HTTP 服务器（独立于通知提供程序，先启动监听）
        services.AddHostedService<HttpNotificationServer>();

        // 4. 注册全局热键警报服务（通知提供方，带提醒设置控件）
        services.AddNotificationProvider<AlertHotkeyService, AlertHotkeySettingsControl>();

        // 4b. 注册下课倒计时提醒服务（通知提供方，带提醒设置控件）
        services.AddNotificationProvider<ClassEndingReminderService, ClassEndingReminderSettingsControl>();

        // 5. 注册小说阅读器组件（带设置面板）
        services.AddComponent<NovelReaderComponent, NovelReaderSettingsControl>();

        // 6. 注册口头禅记录组件（带设置面板）
        services.AddComponent<CatchphraseComponent, CatchphraseComponentSettingsControl>();

        // 6b. 注册头像课程表组件（带设置面板）
        services.AddComponent<AvatarClassScheduleComponent, AvatarClassScheduleSettingsControl>();

        // 6c. 注册 RSS 新闻组件（带设置面板）
        services.AddComponent<RssComponent, RssSettingsControl>();

        // 6d. 注册体育赛事组件（带设置面板）
        services.AddComponent<SportsComponent, SportsSettingsControl>();

        // 6e. 注册摄像头安全检测服务（单例）
        services.AddSingleton<CameraMonitorService>();

        // 6f. 注册摄像头安全指示器组件
        services.AddComponent<CameraStatusComponent>();

        // 6d. 注册点名器通知提供程序
        services.AddNotificationProvider<LuckyPickerNotifier>();
        // 额外注册为自身类型，以便 TryGetService 能解析（AddNotificationProvider 只注册为 IHostedService）
        services.AddSingleton(sp => sp.GetServices<IHostedService>().OfType<LuckyPickerNotifier>().First());

        // 7. 注册 ClassIsland 自动化行动（可在自动化规则中使用）

        // 全局组件显隐
        services.AddAction<ToggleAllVisibilityAction>();
        services.AddAction<ShowAllComponentsAction>();
        services.AddAction<HideAllComponentsAction>();

        // 小说阅读器
        services.AddAction<NovelPauseAction>();
        services.AddAction<NovelResumeAction>();
        services.AddAction<NovelNextPageAction>();
        services.AddAction<NovelPrevPageAction>();
        services.AddAction<NovelRestartAction>();

        // RSS 新闻
        services.AddAction<RssNextAction>();
        services.AddAction<RssPrevAction>();

        // 口头禅
        services.AddAction<CatchphraseClearAction>();

        // 摄像头监控 Actions
        services.AddAction<ToggleCameraMonitorAction>();
        services.AddAction<EnableCameraMonitorAction>();
        services.AddAction<DisableCameraMonitorAction>();

        // 摄像头监控 Triggers（"当事件触发时"）
        services.AddTrigger<CameraInUseTrigger>();
        services.AddTrigger<CameraStoppedTrigger>();

        // 8. 注册设置页面
        services.AddSettingsPage<MySettingsPage>();
        services.AddSettingsPage<LuckyPickerSettingsPage>();

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
                if (Settings.LuckyPicker.IsEnabled)
                {
                    var luckySettings = Settings.LuckyPicker;
                    var service = new LuckyPickerService(luckySettings);
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
}
