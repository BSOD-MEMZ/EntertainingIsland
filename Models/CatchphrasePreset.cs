using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace EntertainingIsland.Models;

/// <summary>
/// 口头禅预设条目：一个短语 + 绑定的快捷键
/// </summary>
public partial class CatchphrasePreset : ObservableObject
{
    [ObservableProperty]
    private string _phrase = "";

    [ObservableProperty]
    private HotkeyConfig _hotkey = new() { Ctrl = true, Shift = false, Alt = false, Win = false, Key = "F1" };
}
