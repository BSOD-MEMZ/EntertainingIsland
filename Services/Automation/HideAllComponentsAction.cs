using System.Threading.Tasks;
using ClassIsland.Core.Abstractions.Automation;
using ClassIsland.Core.Attributes;
using ClassIsland.Shared;

namespace EntertainingIsland.Services.Automation;

[ActionInfo("entertainingisland.hide-all", "隐藏所有组件", "\uE817", defaultGroupToMenu: "EntertainingIsland")]
public class HideAllComponentsAction : ActionBase
{
    protected override Task OnInvoke()
    {
        var s = IAppHost.TryGetService<EntertainmentState>();
        if (s != null) s.IsHidden = true;
        return Task.CompletedTask;
    }

    protected override Task OnRevert()
    {
        var s = IAppHost.TryGetService<EntertainmentState>();
        if (s != null) s.IsHidden = false;
        return Task.CompletedTask;
    }
}
