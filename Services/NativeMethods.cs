using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace EntertainingIsland.Services;

/// <summary>
/// 跨平台原生 API 声明。Windows 使用 user32.dll，Linux 使用 libX11。
/// </summary>
internal static partial class NativeMethods
{
    // ==================== Windows 热键 ====================

    [SupportedOSPlatform("windows")]
    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [SupportedOSPlatform("windows")]
    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    // ==================== Linux X11 热键 ====================

    private const string Xlib = "libX11.so.6";

    /// <summary>X11 修饰键掩码</summary>
    public const uint X11ShiftMask   = 1 << 0;
    public const uint X11LockMask    = 1 << 1;
    public const uint X11ControlMask = 1 << 2;
    public const uint X11Mod1Mask    = 1 << 3;  // Alt
    public const uint X11Mod2Mask    = 1 << 4;  // NumLock
    public const uint X11Mod3Mask    = 1 << 5;
    public const uint X11Mod4Mask    = 1 << 6;  // Super (Win)
    public const uint X11Mod5Mask    = 1 << 7;

    public const int X11KeyPress   = 2;
    public const int X11GrabModeAsync = 1;

    // ---- 线程安全与错误处理 ----

    [SupportedOSPlatform("linux")]
    [DllImport(Xlib)]
    public static extern int XInitThreads();

    /// <summary>X11 错误处理器委托</summary>
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int X11ErrorHandlerDelegate(IntPtr display, ref X11ErrorEvent error);

    [SupportedOSPlatform("linux")]
    [DllImport(Xlib)]
    public static extern IntPtr XSetErrorHandler(X11ErrorHandlerDelegate handler);

    /// <summary>X11 错误事件结构</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct X11ErrorEvent
    {
        public int type;
        public IntPtr display;
        public IntPtr resourceid;
        public IntPtr serial;
        public byte error_code;
        public byte request_code;
        public byte minor_code;
    }

    /// <summary>
    /// 安装安全的 X11 错误处理器（替换默认的 exit(1) 行为）。
    /// </summary>
    private static bool _x11ErrorHandlerInstalled;

    [SupportedOSPlatform("linux")]
    public static void InstallX11ErrorHandler()
    {
        if (_x11ErrorHandlerInstalled) return;
        _x11ErrorHandlerInstalled = true;

        XInitThreads();
        XSetErrorHandler((IntPtr _, ref X11ErrorEvent err) =>
        {
            // 仅记录，绝不崩溃
            Logger.Warn($"[X11] 错误 code={err.error_code} request={err.request_code} minor={err.minor_code}");
            return 0;
        });
    }

    // ---- Display / Key ----

    [SupportedOSPlatform("linux")]
    [DllImport(Xlib)]
    public static extern IntPtr XOpenDisplay(string? name);

    [SupportedOSPlatform("linux")]
    [DllImport(Xlib)]
    public static extern int XCloseDisplay(IntPtr display);

    [SupportedOSPlatform("linux")]
    [DllImport(Xlib)]
    public static extern IntPtr XDefaultRootWindow(IntPtr display);

    [SupportedOSPlatform("linux")]
    [DllImport(Xlib)]
    public static extern int XGrabKey(IntPtr display, int keycode, uint modifiers, IntPtr grabWindow,
        bool ownerEvents, int pointerMode, int keyboardMode);

    [SupportedOSPlatform("linux")]
    [DllImport(Xlib)]
    public static extern int XUngrabKey(IntPtr display, int keycode, uint modifiers, IntPtr grabWindow);

    [SupportedOSPlatform("linux")]
    [DllImport(Xlib)]
    public static extern int XPending(IntPtr display);

    [SupportedOSPlatform("linux")]
    [DllImport(Xlib)]
    public static extern int XNextEvent(IntPtr display, out X11Event ev);

    [SupportedOSPlatform("linux")]
    [DllImport(Xlib)]
    public static extern IntPtr XStringToKeysym(string name);

    [SupportedOSPlatform("linux")]
    [DllImport(Xlib)]
    public static extern int XKeysymToKeycode(IntPtr display, IntPtr keysym);

    [SupportedOSPlatform("linux")]
    [DllImport(Xlib)]
    public static extern int XFlush(IntPtr display);

    /// <summary>X11 KeyEvent 结构</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct X11KeyEvent
    {
        public int type;
        public IntPtr serial;
        public int send_event;
        public IntPtr display;
        public IntPtr window;
        public IntPtr root;
        public IntPtr subwindow;
        public int time;
        public int x, y;
        public int x_root, y_root;
        public uint state;
        public int keycode;
        public int same_screen;
    }

    /// <summary>X11 通用事件（用于 XNextEvent）</summary>
    [StructLayout(LayoutKind.Explicit)]
    public struct X11Event
    {
        [FieldOffset(0)] public int type;
        [FieldOffset(0)] public X11KeyEvent key;
    }
}

