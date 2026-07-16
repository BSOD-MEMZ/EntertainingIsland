using System.Threading.Tasks;
using ClassIsland.Core.Abstractions.Automation;
using ClassIsland.Core.Attributes;
using ClassIsland.Shared;
using EntertainingIsland.Services;

namespace EntertainingIsland.Services.Automation;

[ActionInfo("entertainingisland.camera-monitor.toggle", "切换摄像头监控", "\uE392", defaultGroupToMenu: "EntertainingIsland/摄像头监控")]
public class ToggleCameraMonitorAction : ActionBase
{
    protected override Task OnInvoke()
    {
        var s = IAppHost.TryGetService<CameraMonitorService>();
        if (s != null) s.Enabled = !s.Enabled;
        return Task.CompletedTask;
    }
}
