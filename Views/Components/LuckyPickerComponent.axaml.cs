using Avalonia.Controls;
using ClassIsland.Core;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Shared;
using EntertainingIsland.Models;
using EntertainingIsland.Services;

namespace EntertainingIsland.Views.Components;

[ComponentInfo(
    "D1E2F3A4-B5C6-7890-1234-567890ABCDEF",
    "点名器",
    "\uE716",
    "随机点名。"
)]
public partial class LuckyPickerComponent : ComponentBase<LuckyPickerSettings>
{
    private LuckyPickerService? _service;
    private LuckyPickerNotifier? _notifier;
    private LuckyPickerSettings? _luckySettings;

    public LuckyPickerComponent()
    {
        InitializeComponent();
    }

    protected override void OnAttachedToVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        var plugin = IAppHost.GetService<Plugin>();
        _luckySettings = plugin.Settings.LuckyPicker;

        // 安全键隐藏
        var state = IAppHost.TryGetService<EntertainmentState>();
        if (state != null)
            state.PropertyChanged += (_, a) =>
            {
                if (a.PropertyName == nameof(EntertainmentState.IsHidden))
                    IsVisible = !state.IsHidden;
            };

        _service = new LuckyPickerService(_luckySettings);
        _notifier = IAppHost.TryGetService<LuckyPickerNotifier>();

        // 恢复上次结果
        if (!string.IsNullOrEmpty(_luckySettings.LastPickedName))
            ResultText.Text = _luckySettings.LastPickedName;
    }

    private void PickOne_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        DoPick(() => _service!.Pick());
    }

    private void PickTwo_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        DoPick(() => _service!.PickTwo());
    }

    private void DoPick(Func<string> pickFunc)
    {
        if (_service == null || _luckySettings == null) return;

        var name = pickFunc();
        if (string.IsNullOrEmpty(name)) return;

        // 名单为空/不足时显示提示
        if (name.StartsWith("("))
        {
            ResultText.Text = name;
            return;
        }

        _luckySettings.LastPickedName = name;
        ResultText.Text = name;

        _notifier?.ShowPickResult(name, _luckySettings.NotificationDurationSeconds);
    }
}
