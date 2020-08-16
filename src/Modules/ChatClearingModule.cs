﻿using System;
using System.ComponentModel;
using System.Linq;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System.Threading.Tasks;
using Astramentis.Attributes;
using Astramentis.Services;
using NLog;
using System.Collections.Generic;

namespace Astramentis.Modules
{
    [Name("Clear")]
    [Summary("Clearing chat messages")]
    public class ChatClearingModule : ModuleBase<SocketCommandContext>
    {
        public DiscordSocketClient DiscordSocketClient { get; set; }

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        // clear chatlogs from server channels
        [Command("clear")]
        [Summary("Clears the last x messages in current channel - default 100")]
        [Alias("clean", "prune")]
        [Syntax("clear {optional: number of messages}")]
        [Example("clear 5")]
        [RequireContext(ContextType.Guild)]
        [RequireBotPermission(ChannelPermission.ManageMessages)]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task ClearChatlogsAsync(int count = 100)
        {
            // get messages from channel
            var channel = Context.Channel as SocketTextChannel;
            var messages = await channel.GetMessagesAsync(count).FlattenAsync();

            // try to locate the server in our database
            var server = DiscordServers.ServerList.Find(x => x.DiscordServerObject == Context.Guild);

            // remove schedule embed message from the messages list, so it doesn't get deleted
            if (server != null && server.EventEmbedMessage != null)
                messages = messages.Where(msg => msg.Id != server.EventEmbedMessage.Id);

            // remove reminder messages from the messages list, so they don't get deleted
            if (server != null && server.Events.Exists(x => x.AlertMessage != null))
                messages = messages.Where(msg => server.Events.Any(x => msg.Id != x.AlertMessage.Id));

            messages = messages.ToList();

            // delete the command message
            await Context.Message.DeleteAsync();

            Logger.Log(LogLevel.Info, $"Deleting {messages.Count()} messages in the channel {channel.Name} in the server {channel.Guild.Name}.");

            try
            {
                // bulk delete messages - only works on messages less than two weeks old
                await channel.DeleteMessagesAsync(messages);
            }
            catch
            {
                Logger.Log(LogLevel.Info, "Could not bulk delete messages, switching to individual deletion");

                // get messages older than two weeks, which cannot be bulk-deleted, and new messages that can be bulk-deleted
                var oldMessages = messages.Where(msg => msg.Timestamp < DateTimeOffset.Now.AddDays(-14));
                var newMessages = messages.Where(msg => msg.Timestamp > DateTimeOffset.Now.AddDays(-14));

                if (oldMessages.Any())
                {
                    // notify the user that they started up a manual delete
                    var responseMsg =
                        await ReplyAsync(
                            "Some of the messages you selected are older than two weeks, so we have to individually delete them. This will take a minute.");

                    // don't delete the notification yet
                    newMessages = newMessages.Where(msg => msg.Id != responseMsg.Id);

                    // bulk delete whatever new messages we can
                    await channel.DeleteMessagesAsync(newMessages);

                    // individually delete old messages
                    foreach (var oldMessage in oldMessages)
                    {
                        await oldMessage.DeleteAsync();
                        await Task.Delay(1000);
                    }

                    // done with deleting stuff, so delete the notification
                    await responseMsg.DeleteAsync();
                }
            }
        }

        // clear chatlogs from DMs
        [Command("clear")]
        [Summary("Clears the last x messages in current channel - default 100")]
        [Alias("clean", "prune")]
        [Syntax("clear {optional: number of messages}")]
        [Example("clear 5")]
        [RequireContext(ContextType.DM)]
        public async Task ClearDMChatlogsAsync(int count = 100)
        {
            var channel = Context.Channel as SocketDMChannel;
            var messages = await channel.GetMessagesAsync().FlattenAsync();

            var botMessages = messages.Where(x => x.Author.Id == DiscordSocketClient.CurrentUser.Id).Take(count).ToList();

            Logger.Log(LogLevel.Info, $"Deleting {botMessages.Count} messages in a DM with {channel.Recipient}.");

            // individually delete old messages
            foreach (var message in botMessages)
            {
                await message.DeleteAsync();
                await Task.Delay(1000);
            }
        }
    }
}
