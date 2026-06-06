using System.Threading.Tasks;
using ClassIsland.Core.Abstractions.Automation;
using ClassIsland.Core.Attributes;
using ClassIsland.Shared;

namespace EntertainingIsland.Services.Automation;

[ActionInfo("entertainingisland.novel.prev-page", "小说上一页", "\uE0AA", defaultGroupToMenu: "EntertainingIsland/小说阅读器")]
public class NovelPrevPageAction : ActionBase
{
    protected override Task OnInvoke()
    {
        var s = IAppHost.TryGetService<EntertainmentState>();
        if (s != null) s.NovelPrevPageRequest = !s.NovelPrevPageRequest;
        return Task.CompletedTask;
    }
}
