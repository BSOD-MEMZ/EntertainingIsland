using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.Win32;
using ClassIsland.Core;
using EntertainingIsland.Helpers;
using EntertainingIsland.Models;

namespace EntertainingIsland.Services;

/// <summary>
/// 全局热键管理器 — 统一管理 RegisterHotKey/UnregisterHotKey、窗口句柄重试、WndProc 钩子。
/// 消除 NovelReaderComponent / RssComponent / SportsComponent 中的重复热键代码。
/// </summary>
public sealed class HotkeyManager : IDisposable
{
    private const int WM_HOTKEY = 0x0312;

    private readonly List<Entry> _entries = new();
    private readonly Action<int> _onHotkey;
    private System.Timers.Timer? _retry;
    private bool _registered;
    private bool _hooked;
    private bool _disposed;

    /// <param name="onHotkey">热键触发回调，参数为热键 ID</param>
    public HotkeyManager(Action<int> onHotkey)
    {
        _onHotkey = onHotkey;
    }

    /// <summary>注册一个热键条目</summary>
    public void Add(int id, HotkeyConfig config)
    {
        _entries.Add(new Entry(id, config));
    }

    /// <summary>启动：立即尝试注册，并启动 2 秒重试定时器</summary>
    public void Start()
    {
        if (_disposed) return;
        _retry = new System.Timers.Timer(2000);
        _retry.Elapsed += (_, _) => Dispatcher.UIThread.Post(TryRegister);
        _retry.AutoReset = true;
        _retry.Start();
        Dispatcher.UIThread.Post(TryRegister);
    }

    /// <summary>停止并注销所有热键</summary>
    public void Stop()
    {
        _retry?.Stop();
        _retry?.Dispose();
        _retry = null;
        UnregisterAll();
    }

    /// <summary>设置变更时刷新：注销后重新注册</summary>
    public void Refresh()
    {
        Dispatcher.UIThread.Post(() =>
        {
            UnregisterAll();
            TryRegister();
        });
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Stop();
    }

    private void TryRegister()
    {
        if (_registered) { _retry?.Stop(); return; }
        if (_entries.Count == 0) return;

        var w = AppBase.Current.MainWindow ?? AppBase.Current.GetRootWindow();
        if (w == null) return;
        var ph = w.TryGetPlatformHandle();
        if (ph == null) return;

        var hwnd = ph.Handle;
        var anyOk = false;

        foreach (var e in _entries)
        {
            var key = HotkeyHelper.FixKey(e.Config.Key);
            var vk = HotkeyHelper.VkFromKey(key);
            var mod = e.Config.GetModifiers();
            if (vk > 0 && NativeMethods.RegisterHotKey(hwnd, e.Id, mod, vk))
                anyOk = true;
        }

        if (anyOk)
        {
            _registered = true;
            _retry?.Stop();
            if (!_hooked)
            {
                _hooked = true;
                Win32Properties.AddWndProcHookCallback(w, WndProc);
            }
        }
    }

    private void UnregisterAll()
    {
        if (!_registered) return;
        var w = AppBase.Current.MainWindow ?? AppBase.Current.GetRootWindow();
        var ph = w?.TryGetPlatformHandle();
        if (ph != null)
        {
            foreach (var e in _entries)
                NativeMethods.UnregisterHotKey(ph.Handle, e.Id);
        }
        _registered = false;
    }

    private IntPtr WndProc(IntPtr h, uint msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg != WM_HOTKEY) return IntPtr.Zero;
        handled = true;
        _onHotkey(wParam.ToInt32());
        return IntPtr.Zero;
    }

    private sealed class Entry
    {
        public int Id { get; }
        public HotkeyConfig Config { get; }

        public Entry(int id, HotkeyConfig config)
        {
            Id = id;
            Config = config;
        }
    }
}
