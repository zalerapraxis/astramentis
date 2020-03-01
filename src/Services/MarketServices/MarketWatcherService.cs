﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Astramentis.Enums;
using Astramentis.Models;
using Astramentis.Services.DatabaseServices;
using Discord;
using Discord.WebSocket;
using NLog;

namespace Astramentis.Services.MarketServices
{
    public class MarketWatcherService
    {
        private readonly DiscordSocketClient _discord;
        private readonly MarketService _marketService;
        private readonly DatabaseMarketWatchlist _databaseMarketWatchlist;
        private readonly APIHeartbeatService _apiHeartbeatService;
        private readonly Random _rng;

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public bool WatchlistMuted = false;
        public int DifferentialCutoff = 30;

        private Timer _watchlistTimer;

        private List<string> worldsToSearch = new List<string>();

        // Set to 1 to (hopefully) force parallel.foreach loops to run one at a time (synchronously)
        // set to -1 for default behavior
        private static ParallelOptions parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = -1 };

        public MarketWatcherService(DiscordSocketClient discord, MarketService marketService, DatabaseMarketWatchlist databaseMarketWatchlist, APIHeartbeatService apiHeartbeatService, Random rng)
        {
            _discord = discord;
            _marketService = marketService;
            _databaseMarketWatchlist = databaseMarketWatchlist;
            _apiHeartbeatService = apiHeartbeatService;
            _rng = rng;

            // build worlds list
            foreach (var world in (Worlds[])Enum.GetValues(typeof(Worlds)))
            {
                worldsToSearch.Add(world.ToString());
            }

            Logger.Log(LogLevel.Debug, $"Watchlist timer started!");
            _watchlistTimer = new Timer(async delegate { await WatchlistTimer(); }, null, 10000, Timeout.Infinite);
        }

        public async Task<List<string>> GetMarketWatchlist()
        {
            var list = new List<string>();
            var watchlist = await _databaseMarketWatchlist.GetWatchlist();
         
            foreach (var item in watchlist)
            {
                list.Add($"{item.itemName} {(item.hqOnly ? "(HQ)" : "")}");
            }

            return list;
        }

        public async Task WatchlistTimer()
        {
            Logger.Log(LogLevel.Debug, $"Watchlist timer ticked.");

            // TODO: add pause function
            // adjust timer & start it again
            var _watchlistTimerInterval = Convert.ToInt32(TimeSpan.FromMinutes(10).TotalMilliseconds) + _rng.Next(-60000, 60000);
            Logger.Log(LogLevel.Debug, $"Next tick at {DateTime.Now.AddMilliseconds(_watchlistTimerInterval):hh:mm:ss tt}");
            _watchlistTimer.Change(_watchlistTimerInterval, Timeout.Infinite);

            if (_apiHeartbeatService.ApiStatus != CustomApiStatus.OK)
            {
                Logger.Log(LogLevel.Debug, "Heartbeat service reports API is down, skipping watchlist check.");
                return;
            }

            // Don't do anything if I've muted the watchlist
            if (WatchlistMuted)
            {
                Logger.Log(LogLevel.Info, "Watchlist is muted, skipping.");
                return;
            }
                

            // grab list of items to check
            var watchlist = await _databaseMarketWatchlist.GetWatchlist();
            if (watchlist.Count == 0)
                return;

            // grab market analyses for items on watchlist
            ConcurrentDictionary<string, MarketItemAnalysisModel> WatchlistDifferentials = new ConcurrentDictionary<string, MarketItemAnalysisModel>();
            var itemTasks = Task.Run(() => Parallel.ForEach(watchlist, parallelOptions, watchlistEntry =>
            {
                var apiResponse =
                    _marketService.CreateMarketAnalysis(watchlistEntry.itemName, watchlistEntry.itemId, worldsToSearch).Result;

                var analysis = apiResponse[2]; // overall analysis
                if (watchlistEntry.hqOnly)
                {
                    analysis = apiResponse[0]; // overwrite with hq analysis if needed
                }

                WatchlistDifferentials.TryAdd(watchlistEntry.itemName, analysis);
            }));
            Task.WaitAll(itemTasks);

            // build embed & format data to send to my dm's
            var dm = await _discord.GetUser(110866678161645568).GetOrCreateDMChannelAsync();
            var embed = new EmbedBuilder();
            foreach (var watchlistEntry in WatchlistDifferentials)
            {
                var entry = watchlistEntry.Value;
                if (entry.Differential > DifferentialCutoff)
                {
                    embed.AddField(new EmbedFieldBuilder()
                    {
                        Name = entry.Name,
                        Value = $"diff: {entry.Differential}% - avg sold: {entry.AvgSalePrice} - avg mrkt: {entry.AvgMarketPrice} - lowest: {entry.LowestPrice} on {entry.LowestPriceServer}"
                    });
                }
            }

            if (embed.Fields.Any())
                await dm.SendMessageAsync(null, false, embed.Build());
        }
    }
}
