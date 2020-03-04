using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Astramentis.Enums;
using Discord.WebSocket;
using NLog;

namespace Astramentis.Services.MarketServices
{
    public class APIHeartbeatService
    {
        private readonly APIRequestService _apiRequestService;
        private readonly Random _rng;

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private Timer _heartbeatTimer;

        public CustomApiStatus ApiStatus;

        // Set to 1 to (hopefully) force parallel.foreach loops to run one at a time (synchronously)
        // set to -1 for default behavior
        private static ParallelOptions parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = -1 };

        public APIHeartbeatService(APIRequestService apiRequestService, Random rng)
        {
            _apiRequestService = apiRequestService;
            _rng = rng;

            Logger.Log(LogLevel.Debug, $"Heartbeat timer started!");
            _heartbeatTimer = new Timer(async delegate { await HeartbeatCheck(); }, null, 0, Timeout.Infinite);
        }

        private async Task HeartbeatCheck()
        {
            Logger.Log(LogLevel.Info, $"Heartbeat check - {_apiRequestService.TotalAPIRequestsMade} API requests made ({_apiRequestService.TotalAPIRequestsMadeSinceHeartbeat} requests in the last hour) - starting server checks.");
            // thread-safe reset 30m counter
            Interlocked.Exchange(ref _apiRequestService.TotalAPIRequestsMadeSinceHeartbeat, 0);

            // figure out each server's status
            ConcurrentDictionary<Worlds, bool> serverStatusTracker = new ConcurrentDictionary<Worlds, bool>();
            var tasks = Task.Run(() => Parallel.ForEach((Worlds[])Enum.GetValues(typeof(Worlds)), parallelOptions, async server =>
            {
                var serverStatusRequestResult = GetCompanionApiStatus(server.ToString()).Result;
                bool serverStatus;

                if (serverStatusRequestResult == CustomApiStatus.OK)
                    serverStatus = true;
                else
                    serverStatus = false;

                serverStatusTracker.TryAdd(server, serverStatus);
            }));
            await Task.WhenAll(tasks);

            var serverUpCount = serverStatusTracker.Count(x => x.Value == true);
            Logger.Log(LogLevel.Info, $"{serverUpCount} of {serverStatusTracker.Count} servers are logged in. ");

            // if any servers are down, try to log them in
            // do it in order with a delay so we don't get banhammered
            if (serverUpCount != serverStatusTracker.Count)
            {
                foreach (var serverStatus in serverStatusTracker.Where(x => x.Value == false))
                {
                    var server = serverStatus.Key;
                    Logger.Log(LogLevel.Info, $"Logging into {server}...");
                    var loginResult = await _apiRequestService.LoginToCompanionApi(server.ToString());

                    Logger.Log(LogLevel.Info, $"Login to {server} was {(loginResult ? "successful" : "unsuccessful")}.");

                    await Task.Delay(_rng.Next(5000, 10000)); // random delay between 5-10 seconds
                }
            }

            // adjust the timer tick to 30m +- 1m interval & start it again
            var _heartbeatTimerInterval = Convert.ToInt32(TimeSpan.FromMinutes(30).TotalMilliseconds) + _rng.Next(-60000, 60000);
            Logger.Log(LogLevel.Debug, $"Next tick at {DateTime.Now.AddMilliseconds(_heartbeatTimerInterval):hh:mm:ss tt}");
            _heartbeatTimer.Change(_heartbeatTimerInterval, Timeout.Infinite);
        }

        public async Task<bool> IsCompanionAPIUsable(string server = null)
        {
            var apiResponse = await GetCompanionApiStatus(server);            

            if (apiResponse != CustomApiStatus.OK)
                return false;

            return true;
        }

        // returns custom api status 
        private async Task<CustomApiStatus> GetCompanionApiStatus(string server = null)
        {
            if (server == null)
                server = "gilgamesh";

            // run test query
            var apiResponse = await _apiRequestService.QueryCustomApiForHistory(_rng.Next(2, 19), server);

            // if apiresponse does not return a status type, then it should be running fine
            if (apiResponse.GetType() != typeof(CustomApiStatus))
                apiResponse = CustomApiStatus.OK;

            // assign publicly available status var 
            ApiStatus = apiResponse;

            // if it does return a status type, pass that back to calling function
            return apiResponse;
        }
    }
}
