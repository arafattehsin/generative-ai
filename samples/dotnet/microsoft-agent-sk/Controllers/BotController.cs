using Microsoft.Agents.Hosting.AspNetCore;
using Microsoft.Agents.Protocols.Primitives;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace microsoft_agent_sk.Controllers
{
    [Route("api/messages")]
    [ApiController]
    public class BotController(IBotHttpAdapter adapter, IBot bot) : ControllerBase
    {
        [HttpPost]
        public Task PostAsync(CancellationToken cancellationToken)
           => adapter.ProcessAsync(Request, Response, bot, cancellationToken);
    }
}
