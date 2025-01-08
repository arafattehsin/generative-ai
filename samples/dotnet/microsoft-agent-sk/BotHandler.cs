using Microsoft.Agents.Protocols.Adapter;
using Microsoft.Agents.Protocols.Primitives;
using microsoft_agent_sk.Agents;
using microsoft_agent_sk.Helpers;

namespace microsoft_agent_sk
{
    public class BotHandler : ActivityHandler
    {
        private readonly TravelAgent _travelAgent;

        public BotHandler(TravelAgent travelAgent)
        {
            _travelAgent = travelAgent;
        }

        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            // Invoke the WeatherForecastAgent to process the message
            var forecastResponse = await _travelAgent.InvokeAgentAsync(turnContext.Activity.Text);
            if (forecastResponse == null)
            {
                await turnContext.SendActivityAsync(MessageFactory.Text("Sorry, I couldn't get the weather forecast at the moment."), cancellationToken);
                return;
            }

            // Create a response message based on the response content type from the WeatherForecastAgent
            IActivity response = forecastResponse.ContentType switch
            {
                FlightResponseContentType.AdaptiveCard => MessageFactory.Attachment(new Attachment()
                {
                    ContentType = "application/vnd.microsoft.card.adaptive",
                    Content = forecastResponse.Content,
                }),
                _ => MessageFactory.Text(forecastResponse.Content),
            };

            // Send the response message back to the user. 
            await turnContext.SendActivityAsync(response, cancellationToken);
        }

        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            // When someone (or something) connects to the bot, a MembersAdded activity is received.
            // For this sample,  we treat this as a welcome event, and send a message saying hello.
            // For more details around the membership lifecycle, please see the lifecycle documentation.
            IActivity message = MessageFactory.Text("Hello. I'm your travel agent. Currently, I am only responsible for the flight tracking updates. I'm here to help!");

            // Send the response message back to the user. 
            await turnContext.SendActivityAsync(message, cancellationToken);
        }
    }
}
