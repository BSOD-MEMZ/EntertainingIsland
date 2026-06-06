using System.Threading.Tasks;
using ClassIsland.Core.Abstractions.Automation;
using ClassIsland.Core.Attributes;
using ClassIsland.Shared;

namespace EntertainingIsland.Services.Automation;

[ActionInfo("entertainingisland.novel.resume", "继续小说", "\uEC2E", defaultGroupToMenu: "EntertainingIsland/小说阅读器")]
public class NovelResumeAction : ActionBase
{
    protected override Task OnInvoke()
    {
        var s = IAppHost.TryGetService<EntertainmentState>();
        if (s != null) s.NovelResumeRequest = !s.NovelResumeRequest;
        return Task.CompletedTask;
    }
}
