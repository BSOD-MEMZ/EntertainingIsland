using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using ClassIsland.Core;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Attributes;
using ClassIsland.Shared;
using ClassIsland.Shared.Enums;
using ClassIsland.Shared.Models.Profile;
using EntertainingIsland.Models;
using EntertainingIsland.Services;

namespace EntertainingIsland.Views.Components;

[ComponentInfo(
    "F1E2D3C4-B5A6-9780-1234-567890ABCDEF",
    "头像课程表",
    "\uECE5",
    "以教师头像展示课程表，支持自定义头像图片、圆角、透明度等效果。"
)]
public partial class AvatarClassScheduleComponent : ComponentBase<AvatarClassScheduleSettings>
{
    private ILessonsService? _lessonsService;
    private Dictionary<Guid, Subject>? _subjectMap;

    public AvatarClassScheduleComponent()
    {
        InitializeComponent();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        _lessonsService = IAppHost.TryGetService<ILessonsService>();

        // 全局安全键隐藏
        var state = IAppHost.TryGetService<EntertainmentState>();
        if (state != null)
            state.PropertyChanged += (_, a) => { if (a.PropertyName == nameof(EntertainmentState.IsHidden)) IsVisible = !state.IsHidden; };

        var profileService = IAppHost.TryGetService<IProfileService>();

        _subjectMap = profileService?.Profile?.Subjects
            .ToDictionary(kv => kv.Key, kv => kv.Value)
            ?? new();

        if (_lessonsService != null)
        {
            _lessonsService.CurrentTimeStateChanged += OnStateChanged;
            _lessonsService.PropertyChanged += OnLessonsPropertyChanged;
        }

        Settings.PropertyChanged += (_, _) => Dispatcher.UIThread.Post(RebuildUi);
        RebuildUi();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        if (_lessonsService != null)
        {
            _lessonsService.CurrentTimeStateChanged -= OnStateChanged;
            _lessonsService.PropertyChanged -= OnLessonsPropertyChanged;
        }
    }

    private void OnStateChanged(object? s, EventArgs e) => Dispatcher.UIThread.Post(RebuildUi);
    private void OnLessonsPropertyChanged(object? s, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ILessonsService.CurrentClassPlan))
            Dispatcher.UIThread.Post(RebuildUi);
    }

    private void RebuildUi()
    {
        try
        {
            ClassListControl.Items.Clear();

            var accentBrush = this.FindResource("SystemAccentColor") is Color accentColor
                ? new SolidColorBrush(accentColor)
                : Brush.Parse("#409FFF");

            var classPlan = _lessonsService?.CurrentClassPlan;
            var validItems = classPlan?.ValidTimeLayoutItems;
            if (validItems == null || validItems.Count == 0)
            {
                if (Settings.ShowPlaceholder)
                    ClassListControl.Items.Add(new TextBlock
                    {
                        Text = Settings.PlaceholderText,
                        FontSize = 13,
                        Opacity = 0.5,
                        VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
                    });
                return;
            }
            var currentState = _lessonsService!.CurrentState;
            var currentItem = _lessonsService.CurrentTimeLayoutItem;
            var now = DateTime.Now.TimeOfDay;

            // 用 StartTime+EndTime 组合做 key，查找对应科目
            var classLookup = classPlan!.Classes
                .Where(c => c.IsEnabled)
                .Select(c => new { c.SubjectId, Item = c.CurrentTimeLayoutItem })
                .GroupBy(x => (x.Item.StartTime, x.Item.EndTime))
                .ToDictionary(g => g.Key, g => g.First().SubjectId);

            // 遍历有效时间点，只取上课类型（TimeType=0）
            var sorted = validItems
                .Where(t => t.TimeType == 0)
                .Select(t =>
                {
                    var key = (t.StartTime, t.EndTime);
                    var subjectId = classLookup.TryGetValue(key, out var id) ? id : t.DefaultClassId;
                    var subject = _subjectMap!.GetValueOrDefault(subjectId);
                    return new { TimeItem = t, Subject = subject };
                })
                .Where(x => x.Subject != null)
                .ToList();

            if (sorted.Count == 0)
            {
                if (Settings.ShowPlaceholder)
                    ClassListControl.Items.Add(new TextBlock
                    {
                        Text = Settings.PlaceholderText,
                        FontSize = 13,
                        Opacity = 0.5,
                        VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
                    });
                return;
            }

            var spacing = Settings.AvatarSpacing;
            var avatarBorders = new List<(Border Border, TranslateTransform Y, ScaleTransform Scale, double TargetOpacity)>();

            foreach (var item in sorted)
            {
                var isCurrent = currentState == TimeState.OnClass
                    && currentItem != null
                    && item.TimeItem.StartTime == currentItem.StartTime
                    && item.TimeItem.EndTime == currentItem.EndTime;
                var isPast = item.TimeItem.EndTime < now;
                var isUpcoming = item.TimeItem.StartTime > now && !isCurrent;

                // 上课时仅显示当前课程
                if (Settings.ShowOnlyCurrentInClass && currentState == TimeState.OnClass && !isCurrent)
                    continue;

                if (Settings.HidePastClasses && isPast)
                    continue;

                // 加载头像图片，已上课按饱和度处理像素
                IImage? avatar = null;
                var avatarPath = Settings.SubjectAvatarMap.GetValueOrDefault(item.Subject!.Name, "");
                if (!string.IsNullOrEmpty(avatarPath) && File.Exists(avatarPath))
                {
                    if (isPast && Settings.PastClassSaturation < 0.999)
                    {
                        try { avatar = LoadWithSaturation(avatarPath, Settings.PastClassSaturation); } catch { }
                    }
                    else
                    {
                        try { avatar = new Bitmap(avatarPath); } catch { }
                    }
                }

                // 状态透明度
                var avatarOpacity = isPast ? Settings.PastClassOpacity
                                   : isUpcoming ? Settings.UpcomingClassOpacity
                                   : 1.0;

                // 状态特定尺寸
                var avatarSize = isPast ? Settings.PastAvatarSize
                               : isCurrent ? Settings.CurrentAvatarSize
                               : Settings.UpcomingAvatarSize;

                // 当前课程不再显示边框
                var borderBrush = Brushes.Transparent;
                var borderThickness = 0.0;

                // 默认头像背景色（无自定义图片时），已上课时应用饱和度
                var defaultBg = accentBrush;
                if (avatar == null && isPast && Settings.PastClassSaturation < 0.999)
                {
                    var c = ((SolidColorBrush)accentBrush).Color;
                    double gray = 0.299 * c.R + 0.587 * c.G + 0.114 * c.B;
                    byte r = (byte)Math.Clamp(gray + Settings.PastClassSaturation * (c.R - gray), 0, 255);
                    byte g = (byte)Math.Clamp(gray + Settings.PastClassSaturation * (c.G - gray), 0, 255);
                    byte b = (byte)Math.Clamp(gray + Settings.PastClassSaturation * (c.B - gray), 0, 255);
                    defaultBg = new SolidColorBrush(new Color(255, r, g, b));
                }

                var avatarBorder = new Border
                {
                    Width = avatarSize,
                    Height = avatarSize,
                    CornerRadius = new CornerRadius(Settings.AvatarCornerRadius),
                    ClipToBounds = true,
                    Margin = new Thickness(0, 0, spacing, 0),
                    BorderBrush = borderBrush,
                    BorderThickness = new Thickness(borderThickness),
                    Opacity = avatarOpacity,
                    Child = avatar != null
                        ? new Image { Source = avatar, Stretch = Stretch.UniformToFill }
                        : new Border
                        {
                            Background = defaultBg,
                            Child = new TextBlock
                            {
                                Text = item.Subject.Initial,
                                HorizontalAlignment = HorizontalAlignment.Center,
                                VerticalAlignment = VerticalAlignment.Center,
                                FontSize = Settings.AvatarSize * 0.35,
                                FontWeight = FontWeight.Bold,
                                Foreground = Brushes.White
                            }
                        }
                };

                // 入场动画准备：更深的下沉起点 + 缩小 + 完全透明
                var translateY = new TranslateTransform(0, 40);
                var scaleBounce = new ScaleTransform(0.7, 0.7);
                var group = new TransformGroup();
                group.Children.Add(scaleBounce);
                group.Children.Add(translateY);
                avatarBorder.RenderTransform = group;
                avatarBorder.RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative);
                avatarBorder.Opacity = 0;

                ClassListControl.Items.Add(avatarBorder);
                avatarBorders.Add((avatarBorder, translateY, scaleBounce, avatarOpacity));
            }

            if (Settings.EnableEntranceAnimation)
                PlayEntranceAnimation(avatarBorders);
        }
        catch (Exception ex)
        {
            // 静默处理，避免组件崩溃影响主界面
            System.Diagnostics.Debug.WriteLine($"[AvatarSchedule] Error: {ex.Message}");
        }
    }

    /// <summary>
    /// 加载图片并应用像素级饱和度调整（0=全灰, 1=原色）。
    /// 通过 WriteableBitmap 直接修改每个像素的 RGB 值实现真正的去饱和度。
    /// </summary>
    private static unsafe IImage LoadWithSaturation(string filePath, double saturation)
    {
        using var stream = File.OpenRead(filePath);
        var wb = WriteableBitmap.Decode(stream);
        using var fb = wb.Lock();
        if (fb.Format != PixelFormat.Rgba8888 && fb.Format != PixelFormat.Bgra8888)
            return new Bitmap(filePath);

        int w = fb.Size.Width, h = fb.Size.Height;
        bool isRgba = fb.Format == PixelFormat.Rgba8888;
        byte* basePtr = (byte*)fb.Address;

        for (int y = 0; y < h; y++)
        {
            uint* row = (uint*)(basePtr + y * fb.RowBytes);
            for (int x = 0; x < w; x++)
            {
                uint p = row[x];
                byte r, g, b, a;
                if (isRgba)
                {
                    r = (byte)(p & 0xFF);
                    g = (byte)((p >> 8) & 0xFF);
                    b = (byte)((p >> 16) & 0xFF);
                    a = (byte)((p >> 24) & 0xFF);
                }
                else
                {
                    b = (byte)(p & 0xFF);
                    g = (byte)((p >> 8) & 0xFF);
                    r = (byte)((p >> 16) & 0xFF);
                    a = (byte)((p >> 24) & 0xFF);
                }

                // 基于亮度公式的饱和度调整
                double gray = 0.299 * r + 0.587 * g + 0.114 * b;
                byte nr = (byte)Math.Clamp(gray + saturation * (r - gray), 0, 255);
                byte ng = (byte)Math.Clamp(gray + saturation * (g - gray), 0, 255);
                byte nb = (byte)Math.Clamp(gray + saturation * (b - gray), 0, 255);

                row[x] = isRgba
                    ? (uint)(nr | (ng << 8) | (nb << 16) | (a << 24))
                    : (uint)(nb | (ng << 8) | (nr << 16) | (a << 24));
            }
        }

        return wb;
    }

    /// <summary>
    /// 头像层叠入场动画：平滑缓出 + 渐显，紧凑交错使得前一个刚浮现后一个便接上。
    /// </summary>
    private async void PlayEntranceAnimation(List<(Border Border, TranslateTransform Y, ScaleTransform Scale, double TargetOpacity)> borders)
    {
        const int staggerMs = 36;   // 紧凑交错：前一个约 10% 进度时下一个启动
        const int durationMs = 400;

        for (int i = 0; i < borders.Count; i++)
        {
            var (border, y, scale, targetOpacity) = borders[i];
            await Task.Delay(staggerMs);
            _ = AnimateEntrance(border, y, scale, targetOpacity, durationMs);
        }
    }

    /// <summary>
    /// 单个头像入场：Y 从 40→0，Scale 从 0.7→1.0，Opacity 从 0→目标值。
    /// 使用 EaseOutBack 曲线实现轻微过冲后回正的自然效果。
    /// </summary>
    private static async Task AnimateEntrance(
        Border border, TranslateTransform y, ScaleTransform scale,
        double targetOpacity, int durationMs)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        while (true)
        {
            double t = Math.Min(1, sw.ElapsedMilliseconds / (double)durationMs);
            double eased = t >= 1 ? 1.0 : EaseOutBack(t);

            // Scale: 0.7 → 1.0
            double s = 0.7 + 0.3 * eased;
            // Y: 40 → 0
            double dy = 40 * (1 - eased);
            // Opacity: 0 → targetOpacity
            double opacity = targetOpacity * eased;

            Dispatcher.UIThread.Post(() =>
            {
                scale.ScaleX = s;
                scale.ScaleY = s;
                y.Y = dy;
                border.Opacity = opacity;
            });

            if (t >= 1) break;
            await Task.Delay(16);  // ~60fps
        }

        // 确保最终值精确
        Dispatcher.UIThread.Post(() =>
        {
            scale.ScaleX = 1;
            scale.ScaleY = 1;
            y.Y = 0;
            border.Opacity = targetOpacity;
        });
    }

    /// <summary>
    /// EaseOutBack 缓出函数：先过冲再回正，比弹性曲线更平滑自然。
    /// </summary>
    private static double EaseOutBack(double t)
    {
        const double c1 = 1.70158;
        const double c3 = c1 + 1;
        return 1 + c3 * Math.Pow(t - 1, 3) + c1 * Math.Pow(t - 1, 2);
    }
}
