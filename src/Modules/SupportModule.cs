using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Astramentis.Attributes;
using Astramentis.Services.DatabaseServiceComponents;
using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;

namespace Astramentis.Modules
{
    [Name("Support")]
    [Summary("Get support for the bot")]
    public class SupportModule : InteractiveBase
    {
        public DiscordSocketClient DiscordSocketClient { get; set; }
        public DatabaseSupport DatabaseSupport { get; set; }
        private readonly IConfigurationRoot _config;

        private ulong discordBotOwnerId;

        public SupportModule(IConfigurationRoot config)
        {
            _config = config;

            discordBotOwnerId = ulong.Parse(_config["discordBotOwnerId"]);
        }

        [Command("support")]
        [Summary("")]
        [Syntax("support {message}")]
        [Example("support Hi, the bot's garbage, please fix")]
        public async Task SendSupportMessage([Remainder] string message)
        {
            await DatabaseSupport.StoreSupportMessage(message, Context);

            var supportMsgToOwnerSb = new StringBuilder();

            supportMsgToOwnerSb.Append($"[{Context.Message.Id}] Support message from { Context.User.Username}");
            if (!Context.IsPrivate)
                supportMsgToOwnerSb.Append($" on {Context.Guild.Name}");
            supportMsgToOwnerSb.Append($": {message}");

            await DiscordSocketClient.GetUser(discordBotOwnerId).SendMessageAsync(supportMsgToOwnerSb.ToString());
        }


        [Command("respond")]
        [Summary("")]
        [Syntax("respond {id} {message}")]
        [Example("respond 749349122942828554 shut the frick up meanie")]
        [RequireOwner]
        public async Task SendResponseMessage(string id, [Remainder] string messageContents)
        {
            var supportMessage = await DatabaseSupport.GetSupportMessage(id);

            if (supportMessage == null)
            {
                await ReplyAsync("That message ID doesn't exist.");
                return;
            }

            var supportMsgChannel = DiscordSocketClient.GetChannel(ulong.Parse(supportMessage.ChannelID)) as ISocketMessageChannel;
            var supportMsgAuthor = DiscordSocketClient.GetUser(ulong.Parse(supportMessage.AuthorID));

            await supportMsgChannel.SendMessageAsync($"{supportMsgAuthor.Mention} {messageContents}");

            await DatabaseSupport.RemoveSupportMessage(supportMessage.MessageID);
        }
    }
}
