using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Astramentis.Services.Logging;
using Microsoft.Extensions.Configuration;
using NLog;

namespace Astramentis.Services
{
    //
    // This service handles building the events embed & reminder alert messages
    //
    public class ScheduleService
    {
        private readonly DiscordSocketClient _discord;
        private readonly IConfiguration _config;

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private int timezoneOffsetFallback = 8;

        public ScheduleService(
            DiscordSocketClient discord,
            IConfigurationRoot config,
            LoggingService logger)
        {
            _config = config;
            _discord = discord;
        }

        // send or modify messages alerting the user that an event will be starting soon
        public async Task HandleReminders(DbDiscordServer server)
        {
            var firstCalendarEvent = server.Events[0];

            // look for any existing reminder messages in the reminders channel that contain the first event's name
            // if one exists, set that message as the first event's alert message
            // only set the first one so repeated event names don't all get assigned this message
            var oldReminderMessage = await GetPreviousReminderMessage(server, firstCalendarEvent.Name);
            if (oldReminderMessage != null && firstCalendarEvent.AlertMessage == null)
                firstCalendarEvent.AlertMessage = oldReminderMessage;

            foreach (var calendarEvent in server.Events)
            {
                // get amount of time between the calendarevent start time and the current time
                // and round it to the nearest 5m interval, so usually on the 5m interval
                var timeStartDelta = RoundToNearestMinutes(calendarEvent.StartDate - GetCurrentTimeAfterOffset(), 5);

                // if it's less than an hour but more than fifteen minutes, and we haven't sent an alert message, send an alert message
                if (timeStartDelta.TotalHours < 1 && timeStartDelta.TotalMinutes > 15)
                {
                    var messageContents =
                        $"{calendarEvent.Name} is starting in {(int)timeStartDelta.TotalMinutes} minutes.";

                    // if there's an alert message already, edit it
                    if (calendarEvent.AlertMessage != null)
                    {
                        await calendarEvent.AlertMessage.ModifyAsync(m => m.Content = messageContents);

                        Logger.Log(LogLevel.Debug, $"DEBUG - {server.ServerName} - Event upcoming (15m - 1h from now) or currently underway, alert message exists, editing it.");
                    }
                    // if there wasn't an alert message, send a new message
                    else
                    {
                        var msg = await server.ReminderChannel.SendMessageAsync(messageContents);
                        calendarEvent.AlertMessage = msg;
                        Logger.Log(LogLevel.Debug, $"DEBUG - {server.ServerName} - Event upcoming (15m - 1h from now) or currently underway, alert message does not exist, sending one.");
                    }
                }

                // if it's less than an hour and less or equal to fifteen minutes, try to modify an existing alert message or send a new one
                if (timeStartDelta.TotalHours < 1 && timeStartDelta.TotalMinutes <= 15)
                {
                    var messageContents = $"{calendarEvent.Name} is starting shortly ({(int)timeStartDelta.TotalMinutes}m).";

                    // if there's an alert message already, edit it
                    if (calendarEvent.AlertMessage != null)
                    {
                        await calendarEvent.AlertMessage.ModifyAsync(m => m.Content = messageContents);
                    }
                    // if there wasn't an alert message, send a new message
                    else
                    {
                        var msg = await server.ReminderChannel.SendMessageAsync(messageContents);
                        calendarEvent.AlertMessage = msg;
                    }
                }

                // if the event is currently active (after start date but before end date)
                // update the alert message to reflect how much time is left until the event is over
                if (calendarEvent.StartDate < GetCurrentTimeAfterOffset() &&
                    calendarEvent.EndDate > GetCurrentTimeAfterOffset())
                {
                    // get amount of time between the current time and the calendarevent end time
                    var timeEndDelta = RoundToNearestMinutes(calendarEvent.EndDate - GetCurrentTimeAfterOffset(), 5);

                    var messageContents = $"{calendarEvent.Name} is underway, ending in" + GetTimeDeltaFormatting(timeEndDelta) + ".";

                    // if there's an alert message already, edit it
                    if (calendarEvent.AlertMessage != null)
                    {
                        await calendarEvent.AlertMessage.ModifyAsync(m => m.Content = messageContents);
                    }
                    // if there wasn't an alert message, send a new message
                    else
                    {
                        var msg = await server.ReminderChannel.SendMessageAsync(messageContents);
                        calendarEvent.AlertMessage = msg;
                    }
                }

                // if the event is almost past, delete the alertmessage
                if (calendarEvent.EndDate < GetCurrentTimeAfterOffset() + TimeSpan.FromMinutes(5))
                {
                    await calendarEvent.AlertMessage.DeleteAsync();
                    calendarEvent.AlertMessage = null;
                }

                // if the event is over an hour from now and an alert message exists, delete it.
                // this should not normally occur as the alertmessage should be deleted just before the event has concluded.
                // If we get here, it typically signifies a timezone error or that the bot was unable to delete the old message.
                if (calendarEvent.StartDate > GetCurrentTimeAfterOffset() + TimeSpan.FromMinutes(60) && calendarEvent.AlertMessage != null)
                {
                    // await calendarEvent.AlertMessage.DeleteAsync();

                    calendarEvent.AlertMessage = null;
                    Logger.Log(LogLevel.Error, 
                        $"{server.ServerName} - {calendarEvent.Name} on {calendarEvent.StartDate.DayOfWeek} at {calendarEvent.StartDate.TimeOfDay} - " + 
                        $"The event start date is over an hour away, we would have deleted the alert message. " +
                        $"This should not occur normally, and typically signifies a timezone error, or that the bot was unable" +
                        $"to delete its previous message.");
                }
            }
        }

        // send or modify embed messages listing upcoming events from the raid calendar
        public async Task SendEvents(DbDiscordServer server)
        {
            // build embed
            var embed = BuildEventsEmbed(server);

            // check if we haven't set an embed message yet
            if (server.EventEmbedMessage == null)
            {
                // try to get a pre-existing event embed
                var oldEmbedMessage = await GetPreviousEmbed(server);

                // if we found a pre-existing event embed, set it as our current event embed message
                // and edit it
                if (oldEmbedMessage != null)
                {
                    server.EventEmbedMessage = oldEmbedMessage;
                    await server.EventEmbedMessage.ModifyAsync(m => { m.Embed = embed; });
                }
                // otherwise, send a new one and set it as our current event embed message
                else
                {
                    // send embed
                    var message = await server.ReminderChannel.SendMessageAsync(null, false, embed);

                    // store message id
                    server.EventEmbedMessage = message;
                }

            } // if we have set a current event embed message, edit it
            else
            {
                await server.EventEmbedMessage.ModifyAsync(m => { m.Embed = embed; });
            }
        }

        // posts the list of future events into the channel that called the command
        public async Task GetEvents(SocketCommandContext context)
        {
            var server = DbDiscordServers.ServerList.Find(x => x.DiscordServerObject == context.Guild);

            // if command context is the reminders channel, there's already an event embed
            // so just update it instead of sending a new embed
            if (context.Channel.Id == server.ReminderChannel.Id)
                await SendEvents(server);
            else // if context is not the reminders channel, send new embed
            {
                var embed = BuildEventsEmbed(server);
                await context.Channel.SendMessageAsync(null, false, embed);
            }
        }

        // put together the events embed & return it to calling method
        private Embed BuildEventsEmbed(DbDiscordServer server)
        {
            EmbedBuilder embedBuilder = new EmbedBuilder();

            // if there are no items in CalendarEvents, build a field stating so
            if (server.Events.Count == 0)
            {
                embedBuilder.AddField("No raids scheduled.", "Go play Maplestory or something.");
            }

            // iterate through each calendar event and build strings from them
            // if there are no events, the foreach loop is skipped, so no need to check
            foreach (var calendarEvent in server.Events)
            {
                // don't add items from the past
                if (calendarEvent.EndDate < GetCurrentTimeAfterOffset())
                    continue;

                // get the time difference between the event and now
                // roundtonearestminutes wrapper will round it to closest 5m interval
                TimeSpan timeDelta = default;

                // holy fucking formatting batman
                StringBuilder stringBuilder = new StringBuilder();

                // if event hasn't started yet
                if (calendarEvent.StartDate > GetCurrentTimeAfterOffset())
                {
                    stringBuilder.AppendLine($"Starts on {calendarEvent.StartDate,0:M/dd} at {calendarEvent.StartDate,0: h:mm tt} {calendarEvent.Timezone} and ends at {calendarEvent.EndDate,0: h:mm tt} {calendarEvent.Timezone}");
                    stringBuilder.Append(":watch: Starts in");
                    timeDelta = RoundToNearestMinutes(calendarEvent.StartDate - GetCurrentTimeAfterOffset(), 5);
                }
                    
                // if event has started but hasn't finished
                else if (calendarEvent.StartDate < GetCurrentTimeAfterOffset() &&
                         calendarEvent.EndDate > GetCurrentTimeAfterOffset())
                {
                    stringBuilder.AppendLine($"Currently underway, ending at {calendarEvent.EndDate,0: h:mm tt} {calendarEvent.Timezone}");
                    stringBuilder.Append(":watch: Ends in");
                    timeDelta = RoundToNearestMinutes(calendarEvent.EndDate - GetCurrentTimeAfterOffset(), 5);
                }


                // get formatting for timedelta
                stringBuilder.Append(GetTimeDeltaFormatting(timeDelta));

                stringBuilder.Append(".");

                // bundle it all together into a line for the embed
                embedBuilder.AddField($"{calendarEvent.Name}", stringBuilder.ToString());
            }

            // add the extra little embed bits
            embedBuilder.WithTitle("Schedule")
                .WithColor(Color.Blue)
                .WithFooter("Synced: ")
                // set the actual datetime value since discord timestamps
                // are timezone-aware (?)
                .WithTimestamp(DateTime.Now);

            // roll it all up and send it to the channel
            var embed = embedBuilder.Build();
            return embed;
        }

        // searches the _reminderChannel for a message from the bot containing an embed (how else can we filter this - title?)
        // if it finds one, return that message to the calling method to be set as _eventEmbedMessage
        private async Task<IUserMessage> GetPreviousEmbed(DbDiscordServer server)
        {
            // get all messages in reminder channel
            // getmessages set to 500 to reduce the amount of times the bot has to repost the embed because ory's
            // group chats too much in their scheduling channel
            var messages = await server.ReminderChannel.GetMessagesAsync(500).FlattenAsync();
            // try to get a pre-existing embed message matching our usual event embed parameters
            // return the results
            try
            {
                var embedMsg = messages.Where(msg => msg.Author.Id == _discord.CurrentUser.Id)
                    .Where(msg => msg.Embeds.Count > 0)
                    .Where(msg => msg.Embeds.First().Title == "Schedule").ToList().First();
                return (IUserMessage)embedMsg;
            }
            catch
            {
                return null;
            }
        }

        // searches the _reminderChannel for a message from the bot containing the passed param
        // (this should be the title of an event for which we are looking for a remindermessage to edit)
        // if it finds one, return that message to the calling method to be modified
        private async Task<IUserMessage> GetPreviousReminderMessage(DbDiscordServer server, string messageContains)
        {
            // get all messages in reminder channel
            IEnumerable<IMessage> messages = null;

            while (messages == null)
            {
                try
                {
                    messages = await server.ReminderChannel.GetMessagesAsync().FlattenAsync();
                }
                catch
                {
                    Logger.Log(LogLevel.Debug, $"There was an error retrieving reminder messages, we're trying again...");
                }
                await Task.Delay(1000);
            }

            // try to get a pre-existing message matching messageContains (so {eventtitle})
            //return the results or null
            var reminderMsg = messages.Where(msg => msg.Author.Id == _discord.CurrentUser.Id).FirstOrDefault(msg => msg.Content.Contains(messageContains));
            return (IUserMessage)reminderMsg;

        }

        private string GetTimeDeltaFormatting(TimeSpan timeDelta)
        {
            StringBuilder stringBuilder = new StringBuilder();

            // days
            if (timeDelta.Days == 1)
                stringBuilder.Append($" {timeDelta.Days} day");
            if (timeDelta.Days > 1)
                stringBuilder.Append($" {timeDelta.Days} days");
            // comma
            if (timeDelta.Days >= 1 && (timeDelta.Hours > 0 || timeDelta.Minutes > 0))
                stringBuilder.Append(",");
            // hours
            if (timeDelta.Hours == 1)
                stringBuilder.Append($" {timeDelta.Hours} hour");
            if (timeDelta.Hours > 1)
                stringBuilder.Append($" {timeDelta.Hours} hours");
            // and
            if (timeDelta.Hours > 0 && timeDelta.Minutes > 0)
                stringBuilder.Append(" and");
            // minutes
            if (timeDelta.Minutes == 1)
                stringBuilder.Append($" {timeDelta.Minutes} minute");
            if (timeDelta.Minutes > 1)
                stringBuilder.Append($" {timeDelta.Minutes} minutes");

            return stringBuilder.ToString();
        }

        public TimeSpan RoundToNearestMinutes(TimeSpan input, int minutes)
        {
            var totalMinutes = (int)(input + new TimeSpan(0, minutes / 2, 0)).TotalMinutes;

            return new TimeSpan(0, totalMinutes - totalMinutes % minutes, 0);
        }

        /// <summary>
        /// DateTime.Now but adjusted for the desired timezone offset set in config
        /// </summary>
        /// <returns></returns>
        public DateTime GetCurrentTimeAfterOffset()
        {
            var offset = GetTimezoneOffset();

            return DateTime.Now - TimeSpan.FromHours(offset);
        }

        /// <summary>
        /// Pulls the desired timezone offset from the config file, as an int.
        /// </summary>
        /// <returns></returns>
        public int GetTimezoneOffset()
        {
            int offset;
            var configParseResult = int.TryParse(_config["timezoneOffset"], out offset);

            if (!configParseResult)
            {
                Logger.Log(LogLevel.Warn, "Timezone offset not set in config file, falling back to UTC-8");
                offset = timezoneOffsetFallback;
            }

            // the calendar API will try to pull values in the local timezone of the host running the application
            // the discord bot host sits in UTC but my dev machine sits in UTC-4
            // so we effectively have to treat Pacific as ET-3 instead of UTC-7: shave off 4 hours.
#if DEBUG
            offset = offset - 4;
#endif

            return offset;
        }
    }
}
