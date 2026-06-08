using System.Collections.Generic;
using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Interactivity;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Shared;
using EntertainingIsland.Models;
using EntertainingIsland.Services;

namespace EntertainingIsland.Views.Components;

public partial class CatchphraseComponentSettingsControl : ComponentBase<CatchphraseComponentSettings>
{
    public ObservableCollection<CatchphrasePreset> GlobalPresets =>
        IAppHost.GetService<Plugin>().Settings.CatchphrasePresets;

    /// <summary>主插件设置（用于绑定全局开关）</summary>
    public Settings PluginSettings => IAppHost.GetService<Plugin>().Settings;

    public static List<string> KeyOptions { get; } = new()
    {
        "A","B","C","D","E","F","G","H","I","J","K","L","M",
        "N","O","P","Q","R","S","T","U","V","W","X","Y","Z",
        "0","1","2","3","4","5","6","7","8","9",
        "F1","F2","F3","F4","F5","F6","F7","F8","F9","F10","F11","F12"
    };

    public CatchphraseComponentSettingsControl()
    {
        InitializeComponent();
        AddBtn.Click += (_, _) =>
        {
            var presets = GlobalPresets;
            presets.Add(new CatchphrasePreset
            {
                Phrase = "新口头禅",
                Hotkey = new HotkeyConfig { Ctrl = true, Shift = false, Key = "F" + (presets.Count + 1) }
            });
        };
        ClearBtn.Click += (_, _) =>
        {
            IAppHost.TryGetService<CatchphraseStore>()?.Clear();
        };
    }

    private void DeletePreset_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is CatchphrasePreset preset)
            GlobalPresets.Remove(preset);
    }
}
