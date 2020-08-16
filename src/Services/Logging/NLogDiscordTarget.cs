using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using NLog;
using NLog.Config;
using NLog.Targets;

namespace Astramentis
{
    [Target("NLogDiscordTarget")]
    public sealed class NLogDiscordTarget : AsyncTaskTarget
    {
        public NLogDiscordTarget()
        {
        }

        [RequiredParameter]
        public DiscordSocketClient DiscordClient { get; set; }

        protected override Task WriteAsyncTask(LogEventInfo logEvent, CancellationToken token)
        {
            string logMessage = this.RenderLogEvent(this.Layout, logEvent);
            IDictionary<string, object> logProperties = this.GetAllProperties(logEvent);
            return SendMessageToBotAdministrator(logMessage, logProperties);
        }

        private async Task SendMessageToBotAdministrator(string message, IDictionary<string, object> properties)
        {
            await DiscordClient.GetUser(110866678161645568).SendMessageAsync(message);
        }
    }
}