using System.Threading.Tasks;
using ClassIsland.Core.Abstractions.Automation;
using ClassIsland.Core.Attributes;
using ClassIsland.Shared;
using EntertainingIsland.Services;

namespace EntertainingIsland.Services.Automation;

[ActionInfo("entertainingisland.camera-monitor.disable", "禁用摄像头监控", "\uE392", defaultGroupToMenu: "EntertainingIsland/摄像头监控")]
public class DisableCameraMonitorAction : ActionBase
{
    protected override Task OnInvoke()
    {
        var s = IAppHost.TryGetService<CameraMonitorService>();
        if (s != null) s.Enabled = false;
        return Task.CompletedTask;
    }
}
