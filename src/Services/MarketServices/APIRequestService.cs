using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Astramentis.Enums;
using Discord.WebSocket;
using Flurl.Http;
using Microsoft.Extensions.Configuration;
using NLog;

namespace Astramentis.Services.MarketServices
{
    public class APIRequestService
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private string _customMarketApiUrl;
        private string _xivApiKey;

        private int exceptionRetryCount = 5; // number of times to retry api requests
        private int exceptionRetryDelay = 1000; // ms delay between retries

        private int concurrentAPIRequests = 0;
        private int concurrentAPIRequestsMax = 20;

        public int TotalAPIRequestsMade = 0;
        public int TotalAPIRequestsMadeSinceHeartbeat = 0;

        public APIRequestService(IConfigurationRoot config)
        {
            _customMarketApiUrl = config["customMarketApiUrl"];
            _xivApiKey = config["xivApiKey"];
        }

        public async Task<dynamic> QueryXivapiWithItemId(int itemId)
        {
            return await PerformXivapiRequest($"https://xivapi.com/item/{itemId}");
        }

        // search XIVAPI using item name - generally used to get an item's ID or to search for items
        public async Task<dynamic> QueryXivapiWithString(string itemName)
        {
            return await PerformXivapiRequest(
                $"https://xivapi.com/search?string={itemName}&indexes=Item&filters=IsUntradable=0&private_key={_xivApiKey}");
        }

        // search XIVAPI using EXACT item name - generally used to get an item's ID
        public async Task<dynamic> QueryXivapiWithStringExact(string itemName)
        {
            return await PerformXivapiRequest(
                $"https://xivapi.com/search?string={itemName}&indexes=Item&string_algo=match&filters=IsUntradable=0&private_key={_xivApiKey}");
        }

        // get current market listings from custom api
        public async Task<dynamic> QueryCustomApiForPrices(int itemId, string server)
        {
            // todo: catch when api fails with code 202 (waiting on se) and retry
            
            var apiResponse = await PerformCustomApiRequest($"{_customMarketApiUrl}/market/?id={itemId}&server={server}");

            if (apiResponse.GetType() == typeof(CustomApiStatus))
                Logger.Log(LogLevel.Error, $"Custom API listing query for {itemId} on {server} gave {apiResponse.ToString()}");

            return apiResponse;
        }

        // get history data from custom api
        public async Task<dynamic> QueryCustomApiForHistory(int itemId, string server)
        {
            // todo: catch when api fails with code 202 (waiting on se) and retry

            var apiResponse = await PerformCustomApiRequest($"{_customMarketApiUrl}/market/history.php?id={itemId}&server={server}");

            if (apiResponse.GetType() == typeof(CustomApiStatus))
                Logger.Log(LogLevel.Error, $"Custom API history query for {itemId} on {server} gave {apiResponse.ToString()}");

            return apiResponse;
        }

        public async Task<bool> LoginToCompanionApi(string server)
        {
            var response =
                await
                    $"{_customMarketApiUrl}/market/scripts/logincmd.php?server={server}"
                        .GetStringAsync();

            if (response.Contains("1"))
                return true;
            else
                return false;
        }

        private async Task<dynamic> PerformXivapiRequest(string url)
        {
            // number of retries attempted
            var i = 0;

            while (i < exceptionRetryCount)
            {
                while (concurrentAPIRequests >= concurrentAPIRequestsMax)
                    await Task.Delay(1000);
                Interlocked.Increment(ref concurrentAPIRequests);

                try
                {
                    dynamic apiResponse = await url.GetJsonAsync();

                    Interlocked.Decrement(ref concurrentAPIRequests);

                    // if our request was a search, this will cover any no results errors
                    if (((IDictionary<String, object>) apiResponse).ContainsKey("Results") &&
                        apiResponse.Results.Count == 0)
                        return CustomApiStatus.NoResults;

                    return apiResponse;
                }
                catch (FlurlHttpException exception)
                {
                    Logger.Log(LogLevel.Error, $"Performing URL request ({url}) resulted in an exception: {exception.Message}");
                    await Task.Delay(exceptionRetryDelay);

                    // slow down further if we're being given a rate limit error
                    if (exception.Call.HttpStatus == (HttpStatusCode)429)
                    {
                        Logger.Log(LogLevel.Warn, $"Performing URL request ({url}) resulted in a rate limit error, delaying 5 seconds.");
                        await Task.Delay(5000);
                    }
                }
                i++;
            }

            // return generic api failure code
            return CustomApiStatus.APIFailure;
        }

        private async Task<dynamic> PerformCustomApiRequest(string url)
        {
            // number of retries attempted
            var i = 0;

            // thread-safe tracking of number of custom API requests made
            Interlocked.Increment(ref TotalAPIRequestsMade);
            Interlocked.Increment(ref TotalAPIRequestsMadeSinceHeartbeat);

            while (i < exceptionRetryCount)
            {
                try
                {
                    var apiResponse = await url.GetJsonAsync();

                    if ((object)apiResponse != null)
                    {
                        // check if custom API handled error - get apiResponse as dict of keyvalue pairs
                        // if the dict contains 'Error' key, it's a handled error
                        if (((IDictionary<String, object>)apiResponse).ContainsKey("Error"))
                        {
                            if (apiResponse.Error == null)
                                return CustomApiStatus.APIFailure;
                            if (apiResponse.Error == "Not logged in")
                                return CustomApiStatus.NotLoggedIn;
                            if (apiResponse.Error == "Under maintenance")
                                return CustomApiStatus.UnderMaintenance;
                            if (apiResponse.Error == "Access denied")
                                return CustomApiStatus.AccessDenied;
                            if (apiResponse.Error == "Service unavailable")
                                return CustomApiStatus.ServiceUnavailable;
                        }

                        // check if prices or history key exists
                        if (((IDictionary<String, object>)apiResponse).ContainsKey("Prices"))
                        {
                            if (apiResponse.Prices == null || apiResponse.Prices.Count == 0)
                                return CustomApiStatus.NoResults;

                            // success, return response
                            return apiResponse;
                        }

                        if (((IDictionary<String, object>) apiResponse).ContainsKey("Sales"))
                        {
                            if (apiResponse.Sales == null || apiResponse.Sales.Count == 0)
                                return CustomApiStatus.NoResults;

                            // success, return response
                            return apiResponse;
                        }

                        // key didn't exist, retry
                    }
                }
                catch (FlurlHttpException exception)
                {
                    Logger.Log(LogLevel.Error, $"{exception.Message}");
                    await Task.Delay(exceptionRetryDelay);
                }
                i++;
            }

            // return generic api failure code
            return CustomApiStatus.APIFailure;
        }
    }
}
