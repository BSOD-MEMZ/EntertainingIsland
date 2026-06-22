using System;
using System.Runtime.Versioning;
using EntertainingIsland.Services;

namespace EntertainingIsland.Helpers;

/// <summary>
/// 热键工具方法——统一所有组件中的 VkFromKey / FixKey 重复代码。
/// 同时支持 Windows 虚拟键码和 Linux X11 KeySym/keycode。
/// </summary>
public static class HotkeyHelper
{
    /// <summary>
    /// 清洗热键输入字符串，防止 ComboBoxItem 等非法内容。
    /// </summary>
    public static string FixKey(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return "J";
        var t = raw.Trim();
        if (t.Contains("ComboBoxItem") || t.Length > 10) return "J";
        if (t.Length >= 2 && t.StartsWith("F", StringComparison.OrdinalIgnoreCase))
            return t.Length <= 4 ? t.ToUpperInvariant() : t[..4].ToUpperInvariant();
        return char.IsLetterOrDigit(t[0]) ? t[..1].ToUpperInvariant() : "J";
    }

    /// <summary>
    /// 将按键名称转为 Windows 虚拟键码。
    /// 支持：A-Z、0-9、F1-F12、LEFT/RIGHT/UP/DOWN/SPACE。
    /// </summary>
    public static uint VkFromKey(string key)
    {
        if (string.IsNullOrEmpty(key)) return 0;
        key = key.ToUpperInvariant().Trim();

        // F1-F12
        if (key.StartsWith('F') && int.TryParse(key[1..], out int fn) && fn is >= 1 and <= 12)
            return (uint)(0x70 + fn - 1);

        // 方向键 & 空格
        return key switch
        {
            "LEFT" => 0x25, "RIGHT" => 0x27, "UP" => 0x26, "DOWN" => 0x28,
            "SPACE" => 0x20,
            _ => SingleCharVk(key)
        };
    }

    private static uint SingleCharVk(string key)
    {
        if (key.Length != 1) return 0;
        return key[0] switch
        {
            >= 'A' and <= 'Z' => (uint)key[0],
            >= '0' and <= '9' => (uint)key[0],
            _ => 0
        };
    }

    // ==================== Linux X11 ====================

    /// <summary>
    /// 将按键名称转为 X11 KeySym 字符串，再通过 XKeysymToKeycode 转为 keycode。
    /// 需要在持有 XOpenDisplay 返回的 display 指针时调用。
    /// </summary>
    [SupportedOSPlatform("linux")]
    public static int X11KeycodeFromKey(IntPtr display, string key)
    {
        if (string.IsNullOrEmpty(key) || display == IntPtr.Zero) return 0;
        key = key.ToUpperInvariant().Trim();

        var keysymName = key switch
        {
            "LEFT" => "Left",
            "RIGHT" => "Right",
            "UP" => "Up",
            "DOWN" => "Down",
            "SPACE" => "space",
            _ => key
        };

        var ks = NativeMethods.XStringToKeysym(keysymName);
        if (ks == IntPtr.Zero) return 0;
        return NativeMethods.XKeysymToKeycode(display, ks);
    }
}
