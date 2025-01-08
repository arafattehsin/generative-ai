using Microsoft.Agents.Authentication;
using Microsoft.Agents.Hosting.AspNetCore;
using Microsoft.Agents.Protocols.Adapter;
using Microsoft.Agents.Protocols.Connector;
using Microsoft.Agents.Protocols.Primitives;

namespace microsoft_agent_sk
{
    public static class ServiceCollectionExtensions
    {
        public static IHostApplicationBuilder AddBot<TBot, THandler>(this IHostApplicationBuilder builder)
            where TBot : IBot
            where THandler : class, TBot
        {
            // builder.Services.AddBotAspNetAuthentication(builder.Configuration);

            // Add Connections object to access configured token connections.
            builder.Services.AddSingleton<IConnections, ConfigurationConnections>();

            // Add factory for ConnectorClient and UserTokenClient creation
            builder.Services.AddSingleton<IChannelServiceClientFactory, RestChannelServiceClientFactory>();

            // Add the BotAdapter, this is the default adapter that works with Azure Bot Service and Activity Protocol.
            builder.Services.AddCloudAdapter();

            // Add the Bot,  this is the primary worker for the bot. 
            builder.Services.AddTransient<IBot, THandler>();

            return builder;
        }

    }
}
