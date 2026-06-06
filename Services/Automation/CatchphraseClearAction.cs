using System.Threading.Tasks;
using ClassIsland.Core.Abstractions.Automation;
using ClassIsland.Core.Attributes;
using ClassIsland.Shared;

namespace EntertainingIsland.Services.Automation;

[ActionInfo("entertainingisland.catchphrase.clear", "清空口头禅", "\uE74D", defaultGroupToMenu: "EntertainingIsland/口头禅")]
public class CatchphraseClearAction : ActionBase
{
    protected override Task OnInvoke()
    {
        var s = IAppHost.TryGetService<EntertainmentState>();
        if (s != null) s.CatchphraseClearRequest = !s.CatchphraseClearRequest;
        return Task.CompletedTask;
    }
}
