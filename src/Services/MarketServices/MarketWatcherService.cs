using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Astramentis.Enums;
using Astramentis.Models;
using Astramentis.Services.DatabaseServices;
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

        private async Task WatchlistTimer()
        {
            Logger.Log(LogLevel.Debug, $"Watchlist timer ticked.");
            if (_apiHeartbeatService.ApiStatus != CustomApiStatus.OK)
            {
                Logger.Log(LogLevel.Debug, "Heartbeat service reports API is down, skipping watchlist check.");
                return;
            }
                
            var watchlist = await _databaseMarketWatchlist.GetWatchlist();

            if (watchlist.Count == 0)
                return;

            ConcurrentDictionary<string, MarketItemAnalysisModel> WatchlistDifferentials = new ConcurrentDictionary<string, MarketItemAnalysisModel>();

            // DEBUG
            Stopwatch timer = new Stopwatch();
            timer.Start();

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

            Console.WriteLine($"{WatchlistDifferentials.Count} entries took {timer.ElapsedMilliseconds}ms"); // DEBUG

            foreach (var watchlistEntry in WatchlistDifferentials)
            {
                var entry = watchlistEntry.Value;
                Logger.Log(LogLevel.Debug, $"{entry.Name} - {entry.Differential}% - sale price avg: {entry.AvgSalePrice}, listing price avg: {entry.AvgMarketPrice}");
                if (entry.Differential > 10)
                {
                    var dm = await _discord.GetUser(110866678161645568).GetOrCreateDMChannelAsync();
                    await dm.SendMessageAsync($"{entry.Name} - {entry.Differential}%");
                }
            }

            // adjust timer & start it again
            /* 
            var _watchlistTimerInterval = Convert.ToInt32(TimeSpan.FromMinutes(10).TotalMilliseconds) + _rng.Next(-60000, 60000);
            Logger.Log(LogLevel.Debug, $"Next tick at {DateTime.Now.AddMilliseconds(_watchlistTimerInterval):hh:mm:ss tt}");
            _watchlistTimer.Change(_watchlistTimerInterval, Timeout.Infinite);
            */
        }
    }
}
