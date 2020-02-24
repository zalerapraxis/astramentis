using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.IO;
using System.Threading.Tasks;
using NLog;
using NLog.Targets;

namespace Astramentis
{
    public class LoggingService
    {
        private readonly DiscordSocketClient _discord;
        private readonly CommandService _commands;

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private string _logDirectory { get; }
        private string _logFile => Path.Combine(_logDirectory, "log.txt");

        // DiscordSocketClient and CommandService are injected automatically from the IServiceProvider
        public LoggingService(DiscordSocketClient discord, CommandService commands)
        {
            _logDirectory = Path.Combine(AppContext.BaseDirectory, "logs");
            
            _discord = discord;
            _commands = commands;
            
            _discord.Log += OnLogAsync;
            _commands.Log += OnLogAsync;

            var logConfig = new NLog.Config.LoggingConfiguration();

            var logFile = new NLog.Targets.FileTarget("logFile") { FileName = _logFile, ArchiveEvery = FileArchivePeriod.Day };
            var logConsole = new NLog.Targets.ConsoleTarget("logConsole");


            logConfig.AddRule(LogLevel.Debug, LogLevel.Fatal, logFile);
            logConfig.AddRule(LogLevel.Debug, LogLevel.Fatal, logConsole);

            NLog.LogManager.Configuration = logConfig;
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
