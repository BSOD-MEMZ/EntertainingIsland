using System.Threading.Tasks;
using ClassIsland.Core.Abstractions.Automation;
using ClassIsland.Core.Attributes;
using ClassIsland.Shared;

namespace EntertainingIsland.Services.Automation;

[ActionInfo("entertainingisland.novel.restart", "小说重新开始", "\uE0BD", defaultGroupToMenu: "EntertainingIsland/小说阅读器")]
public class NovelRestartAction : ActionBase
{
    protected override Task OnInvoke()
    {
        var s = IAppHost.TryGetService<EntertainmentState>();
        if (s != null) s.NovelRestartRequest = !s.NovelRestartRequest;
        return Task.CompletedTask;
    }
}
