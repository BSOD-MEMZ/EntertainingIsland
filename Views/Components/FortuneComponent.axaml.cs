using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Shared;
using EntertainingIsland.Models;
using EntertainingIsland.Services;

namespace EntertainingIsland.Views.Components;

[ComponentInfo(
    "A1B2C3D4-E5F6-7890-ABCD-EF1234567890",
    "每日运势",
    "\uEFFF",
    "每日随机宜忌运势，高中生专属趣味运势指南")]
public partial class FortuneComponent : ComponentBase<FortuneComponentSettings>
{
    private FortuneService? _service;

    public FortuneComponent()
    {
        InitializeComponent();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        var state = IAppHost.TryGetService<EntertainmentState>();
        if (state != null)
            state.PropertyChanged += (_, a) =>
            {
                if (a.PropertyName == nameof(EntertainmentState.IsHidden))
                    IsVisible = !state.IsHidden;
            };

        _service = IAppHost.TryGetService<FortuneService>();
        if (_service != null)
        {
            _service.PropertyChanged += (_, a) =>
            {
                if (a.PropertyName is nameof(FortuneService.TodayGood) or nameof(FortuneService.TodayBad))
                    Dispatcher.UIThread.Post(RefreshDisplay);
            };
        }

        RefreshDisplay();
    }

    private void RefreshDisplay()
    {
        if (_service == null) return;

        var good = _service.TodayGood;
        var bad = _service.TodayBad;

        GoodLine.Text = good.Count > 0
            ? "宜 " + string.Join("  ", good.Select(g => $"{g.MainText} · {g.SubText}"))
            : "宜 ——";

        var showOnlyGood = IAppHost.TryGetService<Plugin>()?.Settings.Fortune.ShowOnlyGood == true;
        BadLine.IsVisible = !showOnlyGood;
        BadLine.Text = bad.Count > 0
            ? "忌 " + string.Join("  ", bad.Select(b => $"{b.MainText} · {b.SubText}"))
            : "忌 ——";
    }
}
