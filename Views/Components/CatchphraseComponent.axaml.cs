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
    "记录老师口头禅，显示统计如：保持安静 x5"
)]
public partial class CatchphraseComponent : ComponentBase<CatchphraseComponentSettings>
{
    private CatchphraseStore? _store;

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

        RefreshDisplay();
    }

    protected override void OnDetachedFromVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
    }

    public void RefreshDisplay()
    {
        if (_store != null)
            CatchphraseLabel.Text = _store.GetDisplayText();
    }
}
