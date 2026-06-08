using System;
using Avalonia.Media;
using ClassIsland.Core;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Abstractions.Services.NotificationProviders;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Models.Notification;
using ClassIsland.Shared;
using ClassIsland.Shared.Enums;
using EntertainingIsland.Models;
using EntertainingIsland.Views.Components;

namespace EntertainingIsland.Services;

/// <summary>
/// 下课倒计时提醒服务 — 一比一照搬 ClassIsland 内置"即将上课"提醒样式。
/// Mask 显示双图标 + 文字，Overlay 使用 ClassEndingNotificationControl（左右分栏：倒计时 + 下节课信息）。
/// </summary>
[NotificationProviderInfo(
    "68EFA00E-7700-4037-8F50-D2DF965372E5",
    "下课倒计时提醒",
    "\uF361",
    "在上课即将结束时显示提醒，样式和即将上课提醒一样。"
)]
public class ClassEndingReminderService : NotificationProviderBase<ClassEndingReminderSettings>
{
    /// <summary>静态实例引用，供调试预览使用</summary>
    public static ClassEndingReminderService? Instance { get; private set; }

    private bool _isNotifiedThisClass;
    private ILessonsService? _lessonsService;

    public ClassEndingReminderService()
    {
        Instance = this;
        AppBase.Current.AppStarted += OnInit;
    }

    private void OnInit(object? sender, EventArgs e)
    {
        _lessonsService = IAppHost.GetService<ILessonsService>();

        _lessonsService.PostMainTimerTicked += OnTimerTick;
        _lessonsService.OnBreakingTime += (_, _) =>
        {
            _isNotifiedThisClass = false;
        };

        Logger.Info("[下课倒计时] 服务已就绪（对齐 ClassIsland 即将上课样式）");
    }

    private void OnTimerTick(object? sender, EventArgs e)
    {
        if (Settings.IsEnabled != true || _lessonsService == null)
            return;

        // 仅在上课状态检查
        if (_lessonsService.CurrentState != TimeState.OnClass)
            return;

        var timeLeft = _lessonsService.OnBreakingTimeLeftTime;
        var threshold = TimeSpan.FromSeconds(Settings.DeltaTime);

        if (timeLeft <= TimeSpan.Zero || timeLeft > threshold)
            return;

        if (_isNotifiedThisClass)
            return;

        _isNotifiedThisClass = true;
        ShowReminder();
    }

    /// <summary>
    /// 显示下课倒计时提醒 — 对齐 ClassIsland ClassNotificationProvider.BuildNotificationRequest
    /// </summary>
    private void ShowReminder()
    {
        var maskText = string.IsNullOrWhiteSpace(Settings.MaskText) ? "即将下课" : Settings.MaskText;
        var message = string.IsNullOrWhiteSpace(Settings.OverlayText) ? "" : Settings.OverlayText;

        // 上课结束时间（用于 Overlay EndTime）
        var classEndTime = new DateTime(
            DateOnly.FromDateTime(DateTime.Now),
            TimeOnly.FromTimeSpan(_lessonsService!.CurrentTimeLayoutItem.EndTime));

        // === 找到真正的下节课（跳过课间休息） ===
        var plan = _lessonsService.CurrentClassPlan;
        var items = plan?.ValidTimeLayoutItems;
        var nowItem = _lessonsService.CurrentTimeLayoutItem;
        var profileService = IAppHost.TryGetService<IProfileService>();
        var subjectMap = profileService?.Profile?.Subjects ?? new();

        string nextName = "";
        string nextTeacher = "";
        TimeSpan nextStart = TimeSpan.Zero;
        TimeSpan nextEnd = TimeSpan.Zero;

        if (items != null && nowItem != null)
        {
            // 构建 Class.Classes 的时间→科目查找表
            var classSubjectMap = new Dictionary<(TimeSpan, TimeSpan), Guid>();
            if (plan?.Classes != null)
            {
                foreach (var c in plan.Classes.Where(c => c.IsEnabled))
                {
                    var item = c.CurrentTimeLayoutItem;
                    classSubjectMap[(item.StartTime, item.EndTime)] = c.SubjectId;
                }
            }

            // 找到当前项的索引
            int currentIdx = -1;
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i].StartTime == nowItem.StartTime && items[i].EndTime == nowItem.EndTime)
                { currentIdx = i; break; }
            }

            // 从下一项开始，找第一个有科目的（即为下节课）
            for (int i = currentIdx + 1; i < items.Count; i++)
            {
                var key = (items[i].StartTime, items[i].EndTime);
                var subjectId = classSubjectMap.TryGetValue(key, out var cid)
                    ? cid : items[i].DefaultClassId;

                if (subjectId != Guid.Empty && subjectMap.TryGetValue(subjectId, out var subject))
                {
                    nextName = subject.Name;
                    nextTeacher = subject.GetFirstName();
                    nextStart = items[i].StartTime;
                    nextEnd = items[i].EndTime;
                    break;
                }
            }
        }

        // === Mask: 仿 ClassIsland CreateTwoIconsMask ===
        var maskContent = NotificationContent.CreateTwoIconsMask(
            maskText,
            rightIcon: "\ue9e4",
            factory: x =>
            {
                x.Duration = TimeSpan.FromSeconds(3);
                x.SpeechContent = Settings.EnableSpeech
                    ? $"距下课还剩{Settings.DeltaTime}秒。"
                    : "";
                x.IsSpeechEnabled = Settings.EnableSpeech;
                if (Settings.EnableEmphasisEffect)
                {
                    try
                    {
                        var faTheme = Avalonia.Application.Current?.Styles
                            .OfType<FluentAvalonia.Styling.FluentAvaloniaTheme>()
                            .FirstOrDefault();
                        x.Color = faTheme?.CustomAccentColor != null
                            ? new SolidColorBrush(faTheme.CustomAccentColor.Value)
                            : null;
                    }
                    catch { }
                }
            });

        // === Overlay: 仿 ClassIsland ClassPrepareNotifyOverlay ===
        var overlayControl = new ClassEndingNotificationControl
        {
            Message = message,
            ShowTeacherName = Settings.ShowTeacherName,
            NextSubjectName = nextName,
            NextSubjectTeacherName = nextTeacher,
            NextClassStartTime = nextStart,
            NextClassEndTime = nextEnd
        };

        var overlayContent = new NotificationContent(overlayControl)
        {
            EndTime = classEndTime,
            SpeechContent = Settings.EnableSpeech
                ? $"{message} 下节课是：{nextName}" +
                  (Settings.ShowTeacherName && !string.IsNullOrEmpty(nextTeacher)
                      ? $"，由{nextTeacher}老师任教"
                      : "") + "。"
                : "",
            IsSpeechEnabled = Settings.EnableSpeech
        };

        var request = new NotificationRequest
        {
            MaskContent = maskContent,
            OverlayContent = overlayContent
        };

        ShowNotification(request);
    }

    /// <summary>
    /// 调试功能：手动触发一次下课倒计时预览，不受状态和时间限制。
    /// </summary>
    public void DebugTriggerPreview()
    {
        if (_lessonsService == null)
        {
            Logger.Warn("[下课倒计时] 调试预览失败：LessonsService 未就绪");
            return;
        }

        Logger.Info("[下课倒计时] 调试预览：手动触发下课提醒");
        ShowReminder();
    }
}
