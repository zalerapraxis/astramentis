﻿using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Discord;
using NLog;

namespace Astramentis
{
    public class CommandHandler
    {
        private readonly DiscordSocketClient _discord;
        private readonly CommandService _commands;
        private readonly IConfigurationRoot _config;
        private readonly IServiceProvider _provider;

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        // ulong is command message id, to match up between command received and command executed
        private Dictionary<ulong, Stopwatch> CommandElapsedTimersTracker = new Dictionary<ulong, Stopwatch>();

        // DiscordSocketClient, CommandService, IConfigurationRoot, and IServiceProvider are injected automatically from the IServiceProvider
        public CommandHandler(
            DiscordSocketClient discord,
            CommandService commands,
            IConfigurationRoot config,
            IServiceProvider provider)
        {
            _discord = discord;
            _commands = commands;
            _config = config;
            _provider = provider;

            _discord.MessageReceived += OnMessageReceivedAsync;
            _commands.CommandExecuted += OnCommandCompletion;
        }

        private async Task OnMessageReceivedAsync(SocketMessage s)
        {
            var msg = s as SocketUserMessage;     // Ensure the message is from a user/bot
            if (msg == null) return;
            if (msg.Author.Id == _discord.CurrentUser.Id) return;     // Ignore self when checking commands
            
            var context = new SocketCommandContext(_discord, msg);     // Create the command context

            // Check if the message has a valid command prefix
            int argPos = 0;
            if (msg.HasStringPrefix(_config["prefix"], ref argPos) || msg.HasMentionPrefix(_discord.CurrentUser, ref argPos))
            {
                // Execute the command
                var result = await _commands.ExecuteAsync(context, argPos, _provider);

                // start up timer for this command, add to tracker list
                var timer = new Stopwatch();
                timer.Start();
                CommandElapsedTimersTracker.Add(context.Message.Id, timer);

                // handle command failed
                if (!result.IsSuccess)
                {
                    // don't track unknown commands, like someone sending "..."
                    // don't track commands that just didn't get input correctly
                    if (result.Error != CommandError.UnknownCommand) 
                    {
                        await context.Channel.SendMessageAsync(result.ToString());
                        Logger.Log(LogLevel.Error, $"Command \"{msg}\" failed: {result}");
                    }
                }
            }
        }

        private async Task OnCommandCompletion(Optional<CommandInfo> command, ICommandContext context, IResult result)
        {
            // don't do anything if the command given isn't valid
            if (command.IsSpecified == false)
                return;

            // grab the timer from the tracker list using the message id of the command
            CommandElapsedTimersTracker.TryGetValue(context.Message.Id, out var timer);

            timer?.Stop();
            var cmdResult = new System.Text.StringBuilder();


            // build log message
            cmdResult.Append($"Command \"{command.Value.Name}\"");

            if (context.Guild == null)
                cmdResult.Append($" issued via DM");
            else
                cmdResult.Append($" issued in \"{context.Guild.Name}\"");

            cmdResult.Append(" executed");

            if (result.IsSuccess)
                cmdResult.Append($" successfully");
            else
                cmdResult.Append($" unsuccessfully with error code {result.Error}");

            if (result.Error == CommandError.Exception)
                cmdResult.Append($" ({result.ErrorReason})");

            cmdResult.Append($" in {timer?.ElapsedMilliseconds}ms");

            Logger.Log(LogLevel.Info, cmdResult.ToString());

            // remove this command entry from the time tracker
            CommandElapsedTimersTracker.Remove(context.Message.Id);
        }
    }
}
