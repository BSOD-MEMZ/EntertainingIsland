using System.Threading.Tasks;
using ClassIsland.Core.Abstractions.Automation;
using ClassIsland.Core.Attributes;
using ClassIsland.Shared;

namespace EntertainingIsland.Services.Automation;

[ActionInfo("entertainingisland.toggle-all", "切换所有组件显隐", "\uF367", defaultGroupToMenu: "EntertainingIsland/全局显隐")]
public class ToggleAllVisibilityAction : ActionBase
{
    protected override Task OnInvoke()
    {
        var s = IAppHost.TryGetService<EntertainmentState>();
        if (s != null) s.IsHidden = !s.IsHidden;
        return Task.CompletedTask;
    }
}
