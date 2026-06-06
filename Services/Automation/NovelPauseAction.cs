using System.Threading.Tasks;
using ClassIsland.Core.Abstractions.Automation;
using ClassIsland.Core.Attributes;
using ClassIsland.Shared;

namespace EntertainingIsland.Services.Automation;

[ActionInfo("entertainingisland.novel.pause", "暂停小说", "\uE9A8", defaultGroupToMenu: "EntertainingIsland/小说阅读器")]
public class NovelPauseAction : ActionBase
{
    protected override Task OnInvoke()
    {
        var s = IAppHost.TryGetService<EntertainmentState>();
        if (s != null) s.NovelPauseRequest = !s.NovelPauseRequest;
        return Task.CompletedTask;
    }
}
