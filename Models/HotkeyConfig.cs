using CommunityToolkit.Mvvm.ComponentModel;

namespace EntertainingIsland.Models;

/// <summary>
/// 热键配置模型。表示一个全局热键的组合键设置。
/// </summary>
public partial class HotkeyConfig : ObservableObject
{
    [ObservableProperty]
    private bool _ctrl = true;

    [ObservableProperty]
    private bool _shift = true;

    [ObservableProperty]
    private bool _alt = false;

    [ObservableProperty]
    private bool _win = false;

    [ObservableProperty]
    private string _key = "Space";

    /// <summary>
    /// 获取热键的显示字符串，例如 "Ctrl+Shift+J"
    /// </summary>
    public string DisplayString
    {
        get
        {
            var parts = new List<string>();
            if (Ctrl) parts.Add("Ctrl");
            if (Shift) parts.Add("Shift");
            if (Alt) parts.Add("Alt");
            if (Win) parts.Add("Win");
            parts.Add(Key);
            return string.Join("+", parts);
        }
    }

    /// <summary>
    /// 获取 Windows RegisterHotKey 所需的修饰键标志
    /// </summary>
    public uint GetModifiers()
    {
        uint mod = 0;
        if (Alt) mod |= 0x0001;   // MOD_ALT
        if (Ctrl) mod |= 0x0002;  // MOD_CONTROL
        if (Shift) mod |= 0x0004; // MOD_SHIFT
        if (Win) mod |= 0x0008;   // MOD_WIN
        return mod;
    }

    /// <summary>
    /// 克隆一个副本
    /// </summary>
    public HotkeyConfig Clone()
    {
        return new HotkeyConfig
        {
            Ctrl = this.Ctrl,
            Shift = this.Shift,
            Alt = this.Alt,
            Win = this.Win,
            Key = this.Key
        };
    }
}
