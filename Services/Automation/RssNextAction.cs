using System.Threading.Tasks;
using ClassIsland.Core.Abstractions.Automation;
using ClassIsland.Core.Attributes;
using ClassIsland.Shared;

namespace EntertainingIsland.Services.Automation;

[ActionInfo("entertainingisland.rss.next", "RSS 下一条", "\uE0AB", defaultGroupToMenu: "EntertainingIsland/RSS 新闻")]
public class RssNextAction : ActionBase
{
    protected override Task OnInvoke()
    {
        var s = IAppHost.TryGetService<EntertainmentState>();
        if (s != null) s.RssNextRequest = !s.RssNextRequest;
        return Task.CompletedTask;
    }
}
