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
    "E1F2A3B4-C5D6-7890-1234-567890ABCDEF",
    "点名器通知",
    "\uE716",
    "在点名后显示通知，展示被选中的同学姓名。"
)]
public class LuckyPickerNotifier : NotificationProviderBase
{
    /// <summary>
    /// 显示点名结果通知
    /// </summary>
    public void ShowPickResult(string name, int durationSeconds)
    {
        var dur = TimeSpan.FromSeconds(Math.Max(1, durationSeconds));

        // 系统消息（如"名单为空"）用简单格式
        var isSystemMsg = name.StartsWith("(");
        var title = isSystemMsg ? name : $"{name} 被选中";
        var detail = isSystemMsg ? name : $"{name}";

        try
        {
            var maskContent = NotificationContent.CreateTwoIconsMask(
                title,
                rightIcon: "\ue9e4",
                factory: x =>
                {
                    x.Duration = dur;
                });

            var overlayContent = NotificationContent.CreateSimpleTextContent(
                detail,
                factory: x =>
                {
                    x.Duration = dur;
                });

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
