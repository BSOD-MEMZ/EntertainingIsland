using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace EntertainingIsland.Models;

public partial class Settings : ObservableObject
{
    [ObservableProperty]
    private bool _showWelcomeMessage = true;

    // ===== 全局安全键 =====

    /// <summary>全局安全键：消警 + 隐藏/显示所有娱乐组件，默认 Ctrl+Shift+K</summary>
    [ObservableProperty]
    private HotkeyConfig _dismissHotkey = new() { Ctrl = true, Shift = true, Key = "K" };

    // ===== 口头禅记录 =====

    [ObservableProperty]
    private ObservableCollection<CatchphrasePreset> _catchphrasePresets = new()
    {
        new() { Phrase = "不要讲话", Hotkey = new() { Ctrl = true, Shift = true, Key = "F1" } },
        new() { Phrase = "安静",     Hotkey = new() { Ctrl = true, Shift = true, Key = "F2" } },
        new() { Phrase = "看黑板",   Hotkey = new() { Ctrl = true, Shift = true, Key = "F3" } },
    };

    [ObservableProperty]
    private bool _catchphraseClearOnNewDay = true;

    // ===== 公平点名器 =====

    [ObservableProperty]
    private LuckyPickerSettings _luckyPicker = new();
}
