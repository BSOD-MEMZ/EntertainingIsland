using System.Threading.Tasks;
using ClassIsland.Core.Abstractions.Automation;
using ClassIsland.Core.Attributes;
using ClassIsland.Shared;

namespace EntertainingIsland.Services.Automation;

[ActionInfo("entertainingisland.rss.prev", "RSS 上一条", "\uE0AA", defaultGroupToMenu: "EntertainingIsland/RSS 新闻")]
public class RssPrevAction : ActionBase
{
    protected override Task OnInvoke()
    {
        var s = IAppHost.TryGetService<EntertainmentState>();
        if (s != null) s.RssPrevRequest = !s.RssPrevRequest;
        return Task.CompletedTask;
    }
}
