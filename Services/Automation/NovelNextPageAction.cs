using System.Threading.Tasks;
using ClassIsland.Core.Abstractions.Automation;
using ClassIsland.Core.Attributes;
using ClassIsland.Shared;

namespace EntertainingIsland.Services.Automation;

[ActionInfo("entertainingisland.novel.next-page", "小说下一页", "\uE0AB", defaultGroupToMenu: "EntertainingIsland/小说阅读器")]
public class NovelNextPageAction : ActionBase
{
    protected override Task OnInvoke()
    {
        var s = IAppHost.TryGetService<EntertainmentState>();
        if (s != null) s.NovelNextPageRequest = !s.NovelNextPageRequest;
        return Task.CompletedTask;
    }
}
