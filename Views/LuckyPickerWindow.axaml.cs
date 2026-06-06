using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using ClassIsland.Core;
using ClassIsland.Shared;
using EntertainingIsland.Models;
using EntertainingIsland.Services;

namespace EntertainingIsland.Views;

/// <summary>
/// 屏幕浮窗 — 小巧圆角正方形，点击弹出菜单选择抽一人/抽两人。
/// 可拖拽移动，全局安全键控制显隐。
/// </summary>
public partial class LuckyPickerWindow : Window
{
    private LuckyPickerService? _service;
    private LuckyPickerNotifier? _notifier;
    private LuckyPickerSettings? _settings;
    private Point _dragStartPos;
    private bool _isDragging;

    public LuckyPickerWindow()
    {
        InitializeComponent();

        // 初始位置：屏幕右下角
        var screen = Screens.Primary;
        if (screen != null)
        {
            var bounds = screen.Bounds;
            Position = new PixelPoint(
                bounds.X + bounds.Width - 80,
                bounds.Y + bounds.Height - 160
            );
        }

        // 拖拽事件
        RootPanel.PointerPressed += OnPointerPressed;
        RootPanel.PointerMoved += OnPointerMoved;
        RootPanel.PointerReleased += OnPointerReleased;
    }

    public void Initialize(LuckyPickerSettings settings, LuckyPickerService service, LuckyPickerNotifier? notifier)
    {
        _settings = settings;
        _service = service;
        _notifier = notifier;

        // 全局安全键隐藏
        var state = IAppHost.TryGetService<EntertainmentState>();
        if (state != null)
            state.PropertyChanged += (_, a) =>
            {
                if (a.PropertyName == nameof(EntertainmentState.IsHidden))
                    Dispatcher.UIThread.Post(() => IsVisible = !state.IsHidden);
            };
    }

    private void PickButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (_isDragging) return;
        if (sender is Control ctrl)
            ctrl.ContextFlyout?.ShowAt(ctrl);
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
        if (_isDragging) return;
        if (_service == null || _settings == null) return;

        var name = pickFunc();
        if (string.IsNullOrEmpty(name)) return;

        // 名单为空/不足时通过通知提示
        if (name.StartsWith("("))
        {
            _notifier?.ShowPickResult(name, 3);
            return;
        }

        _settings.LastPickedName = name;

        // 发送 ClassIsland 通知
        _notifier?.ShowPickResult(name, _settings.NotificationDurationSeconds);
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        _dragStartPos = e.GetPosition(this);
        _isDragging = false;
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;

        var current = e.GetPosition(this);
        var dx = Math.Abs(current.X - _dragStartPos.X);
        var dy = Math.Abs(current.Y - _dragStartPos.Y);

        if (!_isDragging && (dx > 3 || dy > 3))
            _isDragging = true;

        if (_isDragging)
        {
            Position = new PixelPoint(
                Position.X + (int)(current.X - _dragStartPos.X),
                Position.Y + (int)(current.Y - _dragStartPos.Y)
            );
        }
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        var wasDragging = _isDragging;
        _isDragging = false;
        if (wasDragging)
        {
            e.Handled = true;
        }
    }
}
