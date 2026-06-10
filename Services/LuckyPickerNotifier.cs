using System;
using Avalonia.Media;
using ClassIsland.Core.Abstractions.Services.NotificationProviders;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Models.Notification;
using Ns = ClassIsland.Shared.Models.Notification;

namespace EntertainingIsland.Services;

/// <summary>
/// 点名器通知提供者——用于在点名后显示 ClassIsland 原生通知。
/// </summary>
[NotificationProviderInfo(
    "B6C101E9-B9BB-46B9-AF69-89BE1072B667",
    "点名器通知",
    "\uE716",
    "在点名后显示通知，展示被选中的同学姓名。"
)]
public class LuckyPickerNotifier : NotificationProviderBase
{
    /// <summary>
    /// 显示点名结果通知
    /// </summary>
    /// <param name="name">被选中的人名或系统消息</param>
    /// <param name="durationSeconds">二级提醒持续秒数</param>
    /// <param name="showOverlay">是否显示持久化二级提醒</param>
    public void ShowPickResult(string name, int durationSeconds, bool showOverlay = true)
    {
        // 系统消息（如"名单为空"）始终用简单格式，无视 showOverlay
        var isSystemMsg = name.StartsWith("(");
        var title = isSystemMsg ? name : $"{name} 被选中";
        var detail = isSystemMsg ? name : $"{name}";

        // 强调提示（Mask）：始终显示，短时闪出
        var emphasisDuration = TimeSpan.FromSeconds(2);

        try
        {
            var maskContent = NotificationContent.CreateTwoIconsMask(
                title,
                rightIcon: "\ue9e4",
                factory: x =>
                {
                    x.Duration = emphasisDuration;
                });

            NotificationContent? overlayContent = null;
            if (showOverlay || isSystemMsg)
            {
                var dur = TimeSpan.FromSeconds(Math.Max(1, durationSeconds));
                overlayContent = NotificationContent.CreateSimpleTextContent(
                    detail,
                    factory: x =>
                    {
                        x.Duration = dur;
                    });
            }

            var request = new NotificationRequest
            {
                MaskContent = maskContent,
                OverlayContent = overlayContent,
                RequestNotificationSettings = new Ns.NotificationSettings
                {
                    IsSettingsEnabled = true,
                    IsNotificationEnabled = true,
                    IsNotificationEffectEnabled = true,
                    IsNotificationTopmostEnabled = true
                }
            };

            ShowNotification(request);
        }
        catch (Exception ex)
        {
            Logger.Error($"[点名器通知] 发送失败: {ex.Message}");
        }
    }
}
