using System.Timers;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Shared;
using EntertainingIsland.Services;
using Timer = System.Timers.Timer;

namespace EntertainingIsland.Views.Components;

[ComponentInfo(
    "B2C3D4E5-F6A7-8901-BCDE-F12345678901",
    "摄像头安全指示器",
    "\uE392",
    "摄像头启用时显示绿色圆点")]
public partial class CameraStatusComponent : ComponentBase
{
    private CameraMonitorService? _service;
    private Timer? _collapseTimer;
    private bool _cameraActive;

    private const double FullSize = 24.0;
    private const double DotSize = 9.0;
    private const double AnimMs = 350;

    public CameraStatusComponent()
    {
        InitializeComponent();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        // 绑定全局安全键：隐藏/显示
        var state = IAppHost.TryGetService<EntertainmentState>();
        if (state != null)
            state.PropertyChanged += (_, a) =>
            {
                if (a.PropertyName == nameof(EntertainmentState.IsHidden))
                    IsVisible = !state.IsHidden;
            };

        // 获取摄像头监控服务
        _service = IAppHost.TryGetService<CameraMonitorService>();
        if (_service != null)
        {
            _service.PropertyChanged += OnCameraStatusChanged;
            Dispatcher.UIThread.Post(RefreshStatus);
        }
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        if (_service != null)
            _service.PropertyChanged -= OnCameraStatusChanged;
        _collapseTimer?.Dispose();
    }

    private void OnCameraStatusChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(CameraMonitorService.IsCameraInUse))
            Dispatcher.UIThread.Post(RefreshStatus);
    }

    private void RefreshStatus()
    {
        if (_service == null) return;
        var camNow = _service.IsCameraInUse;

        if (camNow && !_cameraActive)
            ShowIndicator();
        else if (!camNow && _cameraActive)
            HideIndicator();

        _cameraActive = camNow;
    }

    private void ShowIndicator()
    {
        _collapseTimer?.Stop();
        _collapseTimer?.Dispose();

        CameraIndicator.IsVisible = true;
        CameraIndicator.Width = FullSize;
        CameraIndicator.Height = FullSize;
        CameraIcon.IsVisible = true;
        CameraIcon.Opacity = 1;
        CameraDot.Width = FullSize;
        CameraDot.Height = FullSize;

        // 弹出动画
        if (CameraIndicator.RenderTransform is not ScaleTransform)
            CameraIndicator.RenderTransform = new ScaleTransform(0, 0);

        new Animation
        {
            Duration = TimeSpan.FromMilliseconds(250),
            FillMode = FillMode.Forward,
            Easing = new QuadraticEaseOut(),
            Children =
            {
                new KeyFrame
                {
                    Cue = new Cue(0.0),
                    Setters =
                    {
                        new Setter(ScaleTransform.ScaleXProperty, 0.0),
                        new Setter(ScaleTransform.ScaleYProperty, 0.0)
                    }
                },
                new KeyFrame
                {
                    Cue = new Cue(1.0),
                    Setters =
                    {
                        new Setter(ScaleTransform.ScaleXProperty, 1.0),
                        new Setter(ScaleTransform.ScaleYProperty, 1.0)
                    }
                }
            }
        }.RunAsync(CameraIndicator);

        // 3 秒后缩小为小圆点
        _collapseTimer = new Timer(3000) { AutoReset = false };
        _collapseTimer.Elapsed += (_, _) => Dispatcher.UIThread.Post(CollapseToDot);
        _collapseTimer.Start();
    }

    private void CollapseToDot()
    {
        if (!CameraIndicator.IsVisible) return;

        // 图标淡出
        new Animation
        {
            Duration = TimeSpan.FromMilliseconds(AnimMs),
            FillMode = FillMode.Forward,
            Easing = new QuadraticEaseIn(),
            Children =
            {
                new KeyFrame
                {
                    Cue = new Cue(0.0),
                    Setters = { new Setter(Control.OpacityProperty, 1.0) }
                },
                new KeyFrame
                {
                    Cue = new Cue(1.0),
                    Setters = { new Setter(Control.OpacityProperty, 0.0) }
                }
            }
        }.RunAsync(CameraIcon);

        // 圆点缩小
        new Animation
        {
            Duration = TimeSpan.FromMilliseconds(AnimMs),
            FillMode = FillMode.Forward,
            Easing = new QuadraticEaseIn(),
            Children =
            {
                new KeyFrame
                {
                    Cue = new Cue(0.0),
                    Setters =
                    {
                        new Setter(Ellipse.WidthProperty, FullSize),
                        new Setter(Ellipse.HeightProperty, FullSize)
                    }
                },
                new KeyFrame
                {
                    Cue = new Cue(1.0),
                    Setters =
                    {
                        new Setter(Ellipse.WidthProperty, DotSize),
                        new Setter(Ellipse.HeightProperty, DotSize)
                    }
                }
            }
        }.RunAsync(CameraDot);

        // 容器跟随缩小
        new Animation
        {
            Duration = TimeSpan.FromMilliseconds(AnimMs),
            FillMode = FillMode.Forward,
            Easing = new QuadraticEaseIn(),
            Children =
            {
                new KeyFrame
                {
                    Cue = new Cue(0.0),
                    Setters =
                    {
                        new Setter(Grid.WidthProperty, FullSize),
                        new Setter(Grid.HeightProperty, FullSize)
                    }
                },
                new KeyFrame
                {
                    Cue = new Cue(1.0),
                    Setters =
                    {
                        new Setter(Grid.WidthProperty, DotSize),
                        new Setter(Grid.HeightProperty, DotSize)
                    }
                }
            }
        }.RunAsync(CameraIndicator);
    }

    private void HideIndicator()
    {
        _collapseTimer?.Stop();
        _collapseTimer?.Dispose();
        _collapseTimer = null;

        // 淡出
        new Animation
        {
            Duration = TimeSpan.FromMilliseconds(AnimMs),
            FillMode = FillMode.Forward,
            Easing = new QuadraticEaseOut(),
            Children =
            {
                new KeyFrame
                {
                    Cue = new Cue(0.0),
                    Setters = { new Setter(Control.OpacityProperty, 1.0) }
                },
                new KeyFrame
                {
                    Cue = new Cue(1.0),
                    Setters = { new Setter(Control.OpacityProperty, 0.0) }
                }
            }
        }.RunAsync(CameraIndicator);

        // 动画结束后隐藏并重置
        var hideTimer = new Timer(AnimMs + 60) { AutoReset = false };
        hideTimer.Elapsed += (_, _) => Dispatcher.UIThread.Post(() =>
        {
            CameraIndicator.IsVisible = false;
            CameraIndicator.Opacity = 1;
        });
        hideTimer.Start();
    }
}
