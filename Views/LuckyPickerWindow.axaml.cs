using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
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
    private PixelPoint _dragStartScreenPos;
    private PixelPoint _dragStartWindowPos;
    private bool _isDragging;
    private bool _dragStarted;

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

        // 在 PickButton 上监听拖拽（覆盖整个窗口，触摸屏也能响应）
        PickButton.AddHandler(InputElement.PointerPressedEvent, OnPointerPressed, RoutingStrategies.Tunnel, true);
        PickButton.AddHandler(InputElement.PointerMovedEvent, OnPointerMoved, RoutingStrategies.Tunnel, true);
        PickButton.AddHandler(InputElement.PointerReleasedEvent, OnPointerReleased, RoutingStrategies.Tunnel, true);
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

    private void PickButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_isDragging || _dragStarted) return;
        PickButton.ContextFlyout?.ShowAt(PickButton);
    }

    private void PickOne_Click(object? sender, RoutedEventArgs e)
    {
        DoPick(() => _service!.Pick());
    }

    private void PickTwo_Click(object? sender, RoutedEventArgs e)
    {
        DoPick(() => _service!.PickTwo());
    }

    private void DoPick(Func<string> pickFunc)
    {
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
        _notifier?.ShowPickResult(name, _settings.NotificationDurationSeconds,
            showOverlay: _settings.ShowPersistentOverlay);
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var pt = e.GetPosition(this);
        _dragStartScreenPos = new PixelPoint(
            Position.X + (int)pt.X,
            Position.Y + (int)pt.Y);
        _dragStartWindowPos = Position;
        _isDragging = false;
        _dragStarted = false;
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;

        var pt = e.GetPosition(this);
        var screenX = Position.X + (int)pt.X;
        var screenY = Position.Y + (int)pt.Y;

        var dx = Math.Abs(screenX - _dragStartScreenPos.X);
        var dy = Math.Abs(screenY - _dragStartScreenPos.Y);

        // 触摸屏需要更大阈值防止误触
        if (!_dragStarted && !_isDragging && (dx > 5 || dy > 5))
            _dragStarted = true;

        if (_dragStarted)
        {
            _isDragging = true;
            Position = new PixelPoint(
                _dragStartWindowPos.X + (screenX - _dragStartScreenPos.X),
                _dragStartWindowPos.Y + (screenY - _dragStartScreenPos.Y));
        }
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_isDragging)
        {
            // 发生了拖拽，阻止按钮点击
            _isDragging = false;
            _dragStarted = false;
            e.Handled = true;
        }
        else
        {
            _dragStarted = false;
        }
    }
}
