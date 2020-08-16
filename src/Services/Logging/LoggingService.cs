﻿using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using NLog;
using NLog.LayoutRenderers;
using NLog.Targets;

namespace Astramentis
{
    public class LoggingService
    {
        private readonly DiscordSocketClient _discord;
        private readonly CommandService _commands;
        private readonly IConfigurationRoot _config;

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private string _logDirectory { get; }
        private string _logFile => Path.Combine(_logDirectory, "log.txt");

        // DiscordSocketClient and CommandService are injected automatically from the IServiceProvider
        public LoggingService(DiscordSocketClient discord, CommandService commands, IConfigurationRoot config)
        {
            _logDirectory = Path.Combine(AppContext.BaseDirectory, "logs");
            
            _discord = discord;
            _commands = commands;
            _config = config;
            
            _discord.Log += OnLogAsync;
            _commands.Log += OnLogAsync;

            // used to check validity of Discord ID provided in config file for NLog Discord target
            _discord.Ready += CheckAndConfigureDiscordLogger;

            // implement custom target to send messages to the bot admin via Discord DMs
            Target.Register<Astramentis.NLogDiscordTarget>("NLogDiscordTarget");

            var logConfig = new NLog.Config.LoggingConfiguration();

            // configure how NLog will display log messages
            var logLayout = "${longdate}|${level:uppercase=true}|${logger:shortName=true}|${message}";

            // configure targets for NLog to send messages to
            var logFile = new FileTarget("logFile") { FileName = _logFile, ArchiveEvery = FileArchivePeriod.Day, Layout = logLayout };
            var logConsole = new ConsoleTarget("logConsole") {Layout = logLayout };

            // tell NLog what range of LogLevels to send to each target
            logConfig.AddRule(LogLevel.Debug, LogLevel.Fatal, logFile);
            logConfig.AddRule(LogLevel.Debug, LogLevel.Fatal, logConsole);

            NLog.LogManager.Configuration = logConfig;
        }

        private async Task CheckAndConfigureDiscordLogger()
        {
            var logConfig = LogManager.Configuration;

            var logLayout = "${longdate}|${level:uppercase=true}|${logger:shortName=true}|${message}";

            // check if discordBotOwnerId var is set in config
            var enableDiscordAlertMessages = ulong.TryParse(_config["discordBotOwnerId"], out var discordBotOwnerId);
            if (enableDiscordAlertMessages)
            {
                // check if the user ID is valid
                if (_discord.GetUser(discordBotOwnerId) != null)
                {
                    var logDiscord = new NLogDiscordTarget() { DiscordClient = _discord, DiscordOwnerId = discordBotOwnerId, Layout = logLayout };
                    logConfig.AddRule(LogLevel.Error, LogLevel.Fatal, logDiscord);
                }
                else
                    Console.WriteLine("ERROR: The discordBotOwnerId provided in the config file is invalid. Discord log alerts are disabled.");
            }

            LogManager.Configuration = logConfig;
        }
        
        private async Task OnLogAsync(LogMessage msg)
        {
            Logger.Log(ConvertLogSeverityToLogLevel(msg.Severity), msg.Message);
        }

        // convert discord's logseverity to nlog's loglevel
        private LogLevel ConvertLogSeverityToLogLevel(LogSeverity logSeverity)
        {
            switch (logSeverity)
            {
                case LogSeverity.Critical:  // total failure
                    return LogLevel.Fatal;
                case LogSeverity.Error:     // recoverable failure
                    return LogLevel.Error;
                case LogSeverity.Warning:   // may cause problems
                    return LogLevel.Warn;
                case LogSeverity.Debug:     // debug info without discord.net stuff
                    return LogLevel.Debug;
                case LogSeverity.Info:      // usual level of info
                    return LogLevel.Info;
                case LogSeverity.Verbose:   // everything including discord.net stuff
                    return LogLevel.Trace;
            }

            return LogLevel.Off;
        }
    }
}
