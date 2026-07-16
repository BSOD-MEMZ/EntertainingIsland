using System.Threading.Tasks;
using ClassIsland.Core.Abstractions.Automation;
using ClassIsland.Core.Attributes;
using ClassIsland.Shared;

namespace EntertainingIsland.Services.Automation;

[ActionInfo("entertainingisland.show-all", "显示所有组件", "\uE813", defaultGroupToMenu: "EntertainingIsland/全局显隐")]
public class ShowAllComponentsAction : ActionBase
{
    protected override Task OnInvoke()
    {
        var s = IAppHost.TryGetService<EntertainmentState>();
        if (s != null) s.IsHidden = false;
        return Task.CompletedTask;
    }

    protected override Task OnRevert()
    {
        var s = IAppHost.TryGetService<EntertainmentState>();
        if (s != null) s.IsHidden = true;
        return Task.CompletedTask;
    }
}
