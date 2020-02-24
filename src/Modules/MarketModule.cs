﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Astramentis.Attributes;
using Astramentis.Datasets;
using Astramentis.Models;
using Astramentis.Services;
using Astramentis.Services.MarketServices;
using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Astramentis.Enums;
using Astramentis.Services.DatabaseServices;

namespace Astramentis.Modules
{
    [Name("Market")]
    [Remarks("Get data from FFXIV markets on Aether")]
    public class MarketModule : InteractiveBase
    {
        public MarketService MarketService { get; set; }
        public DatabaseMarketWatchlist DatabaseMarketWatchlist { get; set; }
        public APIRequestService APIRequestService { get; set; }
        public APIHeartbeatService APIHeartbeatService { get; set; }

        private Dictionary<IUser, IUserMessage> _dictFindItemUserEmbedPairs = new Dictionary<IUser, IUserMessage>();

        private Worlds DefaultWorld = Worlds.gilgamesh;

        [Command("market price", RunMode = RunMode.Async)]
        [Alias("mbp")]
        [Summary("Get prices for an item - takes item name or item id")]
        [Example("market price (server) {name/id}")]
        // function will attempt to parse server from searchTerm, no need to make a separate param
        public async Task MarketGetItemPrice([Remainder] string input)
        {
            // check if companion api is down
            if (await IsCompanionAPIUsable() == false)
                return;

            // clean up inputs & parse them out
            var inputs = await SplitCommandInputs(input, InteractiveCommandReturn.Price);

            // get market data
            var marketQueryResults = await MarketService.GetMarketListings(inputs.ItemName, inputs.ItemId, inputs.WorldsToSearch);

            // if no listings found, notify and end
            if (marketQueryResults.Count == 0)
            {
                await ReplyAsync(
                    "No listings found for that item. Either nobody's selling it, or it's not tradable.");
                return;
            }

            // format market data & display
            var pages = new List<PaginatedMessage.Page>();

            var i = 0;
            var itemsPerPage = 12;

            // iterate through the market results, making a page for every (up to) itemsPerPage listings
            while (i < marketQueryResults.Count)
            {
                // pull up to itemsPerPage entries from the list, skipping any from previous iterations
                var currentPageMarketList = marketQueryResults.Skip(i).Take(itemsPerPage);

                StringBuilder sbListing = new StringBuilder();

                // build data for this page
                foreach (var listing in currentPageMarketList)
                {
                    sbListing.Append($"• **{listing.Quantity}** ");

                    if (listing.IsHq)
                        sbListing.Append("**HQ** ");

                    if (listing.Quantity > 1)
                        // multiple units
                        sbListing.Append($"for {listing.CurrentPrice * listing.Quantity} (**{listing.CurrentPrice}** per unit) ");
                    else // single units
                        sbListing.Append($"for **{listing.CurrentPrice}** ");
                    sbListing.Append($"on **{listing.Server}**");
                    sbListing.AppendLine();
                }

                var page = new PaginatedMessage.Page()
                {
                    Fields = new List<EmbedFieldBuilder>()
                    {
                        new EmbedFieldBuilder()
                        {
                            Name = $"{inputs.ItemName}",
                            Value = sbListing
                        }
                    }
                };

                pages.Add(page);

                i = i + itemsPerPage;
            }

            var pager = new PaginatedMessage()
            {
                Pages = pages,
                Author = new EmbedAuthorBuilder()
                {
                    Name = $"{marketQueryResults.Count} Market listing(s) for {inputs.ItemName}",
                },
                ThumbnailUrl = inputs.ItemIconUrl,
                Color = Color.Blue,
                Options = new PaginatedAppearanceOptions()
                {
                    InformationText = "This is an interactive message. Use the reaction emotes to change pages. Use the :1234: emote and then type a number in chat to go to that page.",
                }
            };

            // send reply with these emotes
            await PagedReplyAsync(pager, new ReactionList()
            {
                Forward = true,
                Backward = true,
                First = true,
                Last = true,
                Info = true,
                Jump = true,
            });
        }

        [Command("market history", RunMode = RunMode.Async)]
        [Alias("mbh")]
        [Summary("Get history for an item - takes item name or item id")]
        [Example("market price (server) {name/id}")]
        // function will attempt to parse server from searchTerm, no need to make a separate param
        public async Task MarketGetItemHistory([Remainder] string input)
        {
            // check if companion api is down
            if (await IsCompanionAPIUsable() == false)
                return;

            // clean up inputs & parse them out
            var inputs = await SplitCommandInputs(input, InteractiveCommandReturn.History);

            // get history data
            var historyQueryResults = await MarketService.GetHistoryListings(inputs.ItemName, inputs.ItemId, inputs.WorldsToSearch);

            // if no listings found, notify and end
            if (historyQueryResults.Count == 0)
            {
                await ReplyAsync(
                    "No listings found for that item. Either nobody's selling it, or it's not tradable.");
                return;
            }

            // format history data & display
            var pages = new List<PaginatedMessage.Page>();

            var i = 0;
            var itemsPerPage = 10;

            // iterate through the history results, making a page for every (up to) itemsPerPage listings
            while (i < historyQueryResults.Count)
            {
                // pull up to itemsPerPage entries from the list, skipping any from previous iterations
                var currentPageHistoryList = historyQueryResults.Skip(i).Take(itemsPerPage);

                StringBuilder sbListing = new StringBuilder();

                // build data for this page
                foreach (var listing in currentPageHistoryList)
                {
                    sbListing.Append($"• **{listing.Quantity}** ");

                    if (listing.IsHq)
                        sbListing.Append("**HQ** ");

                    if (listing.Quantity > 1)
                        // multiple units
                        sbListing.Append($"for {listing.SoldPrice * listing.Quantity} (**{listing.SoldPrice}** per unit) ");
                    else // single units
                        sbListing.Append($"for **{listing.SoldPrice}** ");
                    sbListing.AppendLine();
                    sbListing.Append("››› Sold ");
                    sbListing.Append($"on **{listing.Server}** ");
                    sbListing.Append($"at {listing.SaleDate}");
                    sbListing.AppendLine();
                }

                var page = new PaginatedMessage.Page()
                {
                    Fields = new List<EmbedFieldBuilder>()
                    {
                        new EmbedFieldBuilder()
                        {
                            Name = $"{inputs.ItemName}",
                            Value = sbListing
                        }
                    }
                };

                pages.Add(page);

                i = i + itemsPerPage;
            }

            var pager = new PaginatedMessage()
            {
                Pages = pages,
                Author = new EmbedAuthorBuilder()
                {
                    Name = $"{historyQueryResults.Count} History listing(s) for {inputs.ItemName}",
                },
                ThumbnailUrl = inputs.ItemIconUrl,
                Color = Color.Blue
            };

            // send reply with these emotes
            await PagedReplyAsync(pager, new ReactionList()
            {
                Forward = true,
                Backward = true,
                First = true,
                Last = true,
                Info = true,
                Jump = true
            });
        }


        [Command("market analyze", RunMode = RunMode.Async)]
        [Alias("mba")]
        [Summary("Get market analysis for an item")]
        [Example("market analyze {name/id} (server) - defaults to Gilgamesh")]
        // function will attempt to parse server from searchTerm, no need to make a separate param
        public async Task MarketAnalyzeItem([Remainder] string input)
        {
            // check if companion api is down
            if (await IsCompanionAPIUsable() == false)
                return;

            // clean up inputs & parse them out
            var inputs = await SplitCommandInputs(input, InteractiveCommandReturn.Analyze);

            // get analyses
            var marketAnalysis = await MarketService.CreateMarketAnalysis(inputs.ItemName, inputs.ItemId, inputs.WorldsToSearch);
            var hqMarketAnalysis = marketAnalysis[0];
            var nqMarketAnalysis = marketAnalysis[1];

            // format history data & display

            EmbedBuilder analysisEmbedBuilder = new EmbedBuilder();

            // hq stuff if hq analysis exists
            if (hqMarketAnalysis.NumRecentSales != 0)
            {
                StringBuilder hqFieldBuilder = new StringBuilder();
                hqFieldBuilder.AppendLine($"Avg Listed Price: {hqMarketAnalysis.AvgMarketPrice}");
                hqFieldBuilder.AppendLine($"Avg Sale Price: {hqMarketAnalysis.AvgSalePrice}");
                hqFieldBuilder.AppendLine($"Differential: {hqMarketAnalysis.Differential}%");
                hqFieldBuilder.Append("Active:");
                if (hqMarketAnalysis.NumRecentSales >= 5)
                    hqFieldBuilder.AppendLine(" Yes");
                else
                    hqFieldBuilder.AppendLine("No");
                hqFieldBuilder.Append($"Number of sales: {hqMarketAnalysis.NumRecentSales}");
                if (hqMarketAnalysis.NumRecentSales >= 20)
                    hqFieldBuilder.AppendLine("+");
                else
                    hqFieldBuilder.AppendLine("");

                analysisEmbedBuilder.AddField("HQ", hqFieldBuilder.ToString());
            }

            // nq stuff - first line inline=true in case we had hq values
            StringBuilder nqFieldBuilder = new StringBuilder();
            nqFieldBuilder.AppendLine($"Avg Listed Price: {nqMarketAnalysis.AvgMarketPrice}");
            nqFieldBuilder.AppendLine($"Avg Sale Price: {nqMarketAnalysis.AvgSalePrice}");
            nqFieldBuilder.AppendLine($"Differential: {nqMarketAnalysis.Differential}%");
            nqFieldBuilder.Append("Active:");
            if (nqMarketAnalysis.NumRecentSales >= 5)
                nqFieldBuilder.AppendLine(" Yes");
            else
                nqFieldBuilder.AppendLine("No");
            nqFieldBuilder.Append($"Number of sales: {nqMarketAnalysis.NumRecentSales}");
            if (nqMarketAnalysis.NumRecentSales >= 20)
                nqFieldBuilder.AppendLine("+");
            else
                nqFieldBuilder.AppendLine("");

            analysisEmbedBuilder.AddField("NQ", nqFieldBuilder.ToString());

            StringBuilder embedNameBuilder = new StringBuilder();
            embedNameBuilder.Append($"Market analysis for {inputs.ItemName}");
            if (inputs.WorldsToSearch.Count == 1)
                embedNameBuilder.Append($" on {inputs.WorldsToSearch[0]}");

            analysisEmbedBuilder.Author = new EmbedAuthorBuilder()
            {
                Name = embedNameBuilder.ToString()
            };
            analysisEmbedBuilder.ThumbnailUrl = inputs.ItemIconUrl;
            analysisEmbedBuilder.Color = Color.Blue;

            await ReplyAsync(null, false, analysisEmbedBuilder.Build());
        }

        // should be able to accept inputs in any order - if two values are provided, they will be treated as minilvl and maxilvl respectively
        [Command("market exchange", RunMode = RunMode.Async)]
        [Alias("mbe")]
        [Summary("Get best items to spend your tomes/seals on")]
        [Example("market exchange {currency} (server) - defaults to Gilgamesh")]
        // function will attempt to parse server from searchTerm, no need to make a separate param
        public async Task MarketGetBestCurrencyExchangesAsync([Remainder] string input = null)
        {
            if (input == null || !input.Any())
            {
                StringBuilder categoryListBuilder = new StringBuilder();
                categoryListBuilder.AppendLine("These are the categories you can check:");

                categoryListBuilder.AppendLine("gc - grand company seals");
                categoryListBuilder.AppendLine("poetics - i380 crafter mats, more later maybe");
                categoryListBuilder.AppendLine("gemstones - bicolor gemstones from fates");
                categoryListBuilder.AppendLine("nuts - sacks of nuts from hunts :peanut:");
                categoryListBuilder.AppendLine("wgs - White Gatherer Scrip items");
                categoryListBuilder.AppendLine("wcs - White Crafter Scrip items");
                categoryListBuilder.AppendLine("ygs - Yellow Gatherer Scrip items");
                categoryListBuilder.AppendLine("ycs - Yellow Crafter Scrip items");
                categoryListBuilder.AppendLine("tome - tome mats");

                await ReplyAsync(categoryListBuilder.ToString());
                return;
            }

            // check if companion api is down
            if (await IsCompanionAPIUsable() == false)
                return;

            // convert to lowercase so that if user specified server in capitals,
            // it doesn't break our text matching in serverlist and with api request
            input = input.ToLower();

            // clean up input
            var worldsToSearch = GetServer(input, true);
            input = CleanCommandInput(input);

            string category = input;

            // grab data from api
            var currencyDeals = await MarketService.GetBestCurrencyExchange(category, worldsToSearch);

            // keep items that are actively selling, and order by value ratio to put the best stuff to sell on top
            currencyDeals = currencyDeals.Where(x => x.NumRecentSales > 5).OrderByDescending(x => x.ValueRatio).ToList();

            // catch if the user didn't send a good category
            if (currencyDeals.Count == 0 || !currencyDeals.Any())
            {
                await ReplyAsync("You didn't input an existing category. Run the command by itself to get the categories this command can take.");
                return;
            }

            EmbedBuilder dealsEmbedBuilder = new EmbedBuilder();

            foreach (var item in currencyDeals.Take(8))
            {
                StringBuilder dealFieldNameBuilder = new StringBuilder();
                dealFieldNameBuilder.Append($"{item.Name}");

                StringBuilder dealFieldContentsBuilder = new StringBuilder();
                dealFieldContentsBuilder.AppendLine($"Avg Listed Price: {item.AvgMarketPrice}");
                dealFieldContentsBuilder.AppendLine($"Avg Sale Price: {item.AvgSalePrice}");
                dealFieldContentsBuilder.AppendLine($"Currency cost: {item.CurrencyCost}");
                dealFieldContentsBuilder.AppendLine($"Value ratio: {item.ValueRatio:0.000} gil/c");

                dealFieldContentsBuilder.Append($"Recent sales: {item.NumRecentSales}");
                if (item.NumRecentSales >= 20)
                    dealFieldContentsBuilder.AppendLine("+");
                else
                    dealFieldContentsBuilder.AppendLine("");

                if (item.VendorLocation != null)
                    dealFieldContentsBuilder.AppendLine($"Location: {item.VendorLocation}");

                dealsEmbedBuilder.AddField(dealFieldNameBuilder.ToString(), dealFieldContentsBuilder.ToString(), true);
            }


            // build author stuff 
            StringBuilder embedNameBuilder = new StringBuilder();
            embedNameBuilder.Append($"{category}");
            if (worldsToSearch.Count == 1)
                embedNameBuilder.Append($" on {worldsToSearch[0]}");

            var authorurl = "";

            switch (category)
            {
                case "gc":
                    authorurl = "https://xivapi.com/i/065000/065004.png";
                    break;
                case "poetics":
                    authorurl = "https://xivapi.com/i/065000/065023.png";
                    break;
                case "gemstones":
                    authorurl = "https://xivapi.com/i/065000/065071.png";
                    break;
                case "nuts":
                    authorurl = "https://xivapi.com/i/065000/065068.png";
                    break;
                case "wgs":
                    authorurl = "https://xivapi.com/i/065000/065069.png";
                    break;
                case "wcs":
                    authorurl = "https://xivapi.com/i/065000/065070.png";
                    break;
                case "ygs":
                    authorurl = "https://xivapi.com/i/065000/065043.png";
                    break;
                case "ycs":
                    authorurl = "https://xivapi.com/i/065000/065044.png";
                    break;
                case "goetia":
                    authorurl = "https://xivapi.com/i/065000/065066.png";
                    break;
            }

            dealsEmbedBuilder.Author = new EmbedAuthorBuilder()
            {
                Name = embedNameBuilder.ToString(),
                IconUrl = authorurl
            };
            dealsEmbedBuilder.Color = Color.Blue;

            await ReplyAsync("Items are sorted in descending order by their value ratio - items that are better to sell are at the top.", false, dealsEmbedBuilder.Build());
        }


        [Command("market order", RunMode = RunMode.Async)]
        [Alias("mbo")]
        [Summary("Build a list of the lowest market prices for items, ordered by server")]
        [Example("market order {itemname:count, itemname:count, etc...} or marker order {fending, crafting, etc} - add hq to an item name to require high quality")]
        public async Task MarketCrossWorldPurchaseOrderAsync([Remainder] string input = null)
        {
            if (input == null || !input.Any())
            {
                // let the user know they fucked up, or don't
                return;
            }

            // check if companion api is down
            if (await IsCompanionAPIUsable() == false)
                return;

            // convert to lowercase so that if user specified server in capitals,
            // it doesn't break our text matching in serverlist and with api request
            input = input.ToLower();

            // clean up input
            var worldsToSearch = GetServer(input, false);
            input = CleanCommandInput(input);

            var plsWaitMsg = await ReplyAsync("This could take quite a while. Please hang tight.");

            // check if user input a gearset request instead of item lists - no spaces so we avoid catching things like
            // 'facet coat of casting' under the 'casting' gearset
            if (!input.Contains(" ") && MarketOrderGearsetDataset.Gearsets.Any(x => input.Contains(x.Key)))
            {
                var gearset = MarketOrderGearsetDataset.Gearsets.FirstOrDefault(x => x.Key.Equals(input)).Value;

                var i = 1;
                var gearsetInputSb = new StringBuilder();
                foreach (var item in gearset)
                {
                    var count = 1;
                    if (item.Contains("Ring"))
                        count = 2;

                    gearsetInputSb.Append($"{item}:{count}");

                    if (i < gearset.Count)
                        gearsetInputSb.Append(", ");

                    i++;
                }

                input = gearsetInputSb.ToString();
            }

            // split each item:count pairing
            var inputList = input.Split(", ");


            var itemsList = new List<MarketItemCrossWorldOrderModel>();
            foreach (var item in inputList)
            {
                var itemSplit = item.Split(":");

                var itemName = itemSplit[0];
                var NeededQuantity = int.Parse(itemSplit[1]);
                var itemShouldBeHQ = false;

                // replace hq text in itemname var if it exists, and set shouldbehq flag to true
                if (itemName.Contains("hq"))
                {
                    itemShouldBeHQ = true;
                    itemName = itemName.Replace("hq", "").Trim();
                }

                Console.WriteLine($"{itemShouldBeHQ.ToString()}");

                itemsList.Add(new MarketItemCrossWorldOrderModel() { Name = itemName, NeededQuantity = NeededQuantity, ShouldBeHQ = itemShouldBeHQ });
            }

            var results = await MarketService.GetMarketCrossworldPurchaseOrder(itemsList, worldsToSearch);

            // sort the results into different lists, grouped by server
            var purchaseOrder = results.GroupBy(x => x.Server).ToList();

            // to get the overall cost of the order
            var totalCost = 0;
            var purchaseOrderEmbed = new EmbedBuilder();

            foreach (var server in purchaseOrder)
            {
                List<StringBuilder> purchaseOrderFields = new List<StringBuilder>();

                StringBuilder purchaseOrderSb = new StringBuilder();
                // convert this server IGrouping to a list so we can access its values easily
                var serverList = server.ToList();

                // build this server's item list 
                foreach (var item in serverList)
                {
                    var quality = item.IsHQ ? "(HQ)" : "";
                    var entry = $"{item.Name} {quality} - {item.Quantity} for {item.Price} (total: {item.Quantity * item.Price})";

                    // handle field overflow by copying field contents to a list and re-initializing a new one to continue with
                    if (purchaseOrderSb.Length + entry.Length > 1024)
                    {
                        purchaseOrderFields.Add(purchaseOrderSb);
                        purchaseOrderSb = new StringBuilder();
                    }

                    purchaseOrderSb.AppendLine(entry);

                    totalCost += item.Price * item.Quantity;
                }

                // add current purchaseOrder stringbuilder to the fields list
                purchaseOrderFields.Add(purchaseOrderSb);

                foreach (var purchaseOrderField in purchaseOrderFields)
                {
                    var field = new EmbedFieldBuilder();

                    // regular field
                    field.Name = $"{serverList[0].Server}";
                    field.Value = purchaseOrderField.ToString();
                    purchaseOrderEmbed.AddField(field);
                }

                // embed title
                purchaseOrderEmbed.Title = $"Total cost: {totalCost}";

            }

            await plsWaitMsg.DeleteAsync();
            await ReplyAsync("If any items are missing from the list, it's likely they'd take too many purchases to process. Consider using the market price (mbp) command for missing items.", false, purchaseOrderEmbed.Build());
        }

        [Command("market watchlist add", RunMode = RunMode.Async)]
        [Alias("mwa")]
        [RequireOwner]
        [Summary("")]
        [Example("")]
        public async Task MarketWatchlistAdd([Remainder] string input = null)
        {
            // check if companion api is down
            if (await IsCompanionAPIUsable() == false)
                return;

            // clean inputs
            input = input.ToLower();
            input = CleanCommandInput(input);

            // replace hq text in input var if it exists, and set shouldbehq flag to true
            bool inputShouldBeHq = false;
            if (input.Contains("hq"))
            {
                inputShouldBeHq = true;
                input = input.Replace("hq", "").Trim();
            }

            // grab item id 
            var inputId = await MarketService.SearchForItemByNameExact(input);

            // build entry
            var watchlistEntry = new WatchlistEntry()
            {
                itemName = input,
                itemId = inputId[0].ID,
                hqOnly = inputShouldBeHq
            };

            // add to database
            await DatabaseMarketWatchlist.AddToWatchlist(watchlistEntry);

            await ReplyAsync($"Added item {watchlistEntry.itemName} ({watchlistEntry.itemId}) to watchlist.");
        }

        // for use with commands that take item names & potentially server as inputs
        // cleans them up & splits them out, returns null if failure
        private async Task<MarketCommandInputsModel> SplitCommandInputs(string input, InteractiveCommandReturn function)
        {
            // convert to lowercase so that if user specified server in capitals,
            // it doesn't break our text matching in serverlist and with api request
            input = input.ToLower();

            // clean up input
            var worldsToSearch = GetServer(input, false);
            input = CleanCommandInput(input);

            // declar vars to fill 
            int itemId;
            string itemName = "";
            string itemIconUrl = "";

            // try to get an itemid from input - returns null if failure
            var itemIdResponse = await GetItemIdFromInput(input, function, worldsToSearch);

            if (itemIdResponse == null)
                return null;

            itemId = itemIdResponse.Value;

            // TODO: do we need to check the success of this function afterwards?
            var itemDetailsQueryResult = await APIRequestService.QueryXivapiWithItemId(itemId);

            itemName = itemDetailsQueryResult.Name;
            itemIconUrl = $"https://xivapi.com/{itemDetailsQueryResult.Icon}";

            var inputsSplit = new MarketCommandInputsModel()
            {
                ItemName = itemName,
                ItemId = itemId,
                ItemIconUrl = itemIconUrl,
                WorldsToSearch = worldsToSearch
            };

            return inputsSplit;
        }

        // provides an item id for market commands - 
        private async Task<int?> GetItemIdFromInput(string input, InteractiveCommandReturn function, List<string> worldsToSearch)
        {
            int itemId;

            // try to see if the given text is an item ID
            var searchTermIsItemId = int.TryParse(input, out itemId);

            // if user passed a itemname, get corresponding itemid.
            if (!searchTermIsItemId)
            {
                // response is either a ordereddictionary of keyvaluepairs, or null
                var itemIdQueryResult = await MarketService.SearchForItemByName(input);

                // something is wrong with xivapi
                if (itemIdQueryResult == null)
                {
                    await Context.Channel.SendMessageAsync(
                        "Something is wrong with XIVAPI. Try using Garlandtools to get the item's ID and use that instead.");
                    return null;
                }

                // no results
                if (itemIdQueryResult.Count == 0)
                {
                    await Context.Channel.SendMessageAsync("No tradeable items found. Try to expand your search terms, or check for typos. ");
                    return null;
                }
                    

                // too many results
                if (itemIdQueryResult.Count > 15)
                {
                    await Context.Channel.SendMessageAsync("Too many results found. Try narrowing down your search terms.");
                    return null;
                }
                    

                // if more than one result was found, send the results to the selection function to narrow it down to one
                // terminate this function, as the selection function will eventually re-call this method with a single result item
                // 10 is the max number of items we can use interactiveuserselectitem with
                if (itemIdQueryResult.Count > 1 && itemIdQueryResult.Count < 15)
                {
                    await InteractiveUserSelectItem(itemIdQueryResult, function, worldsToSearch);
                    return null;
                }

                // if only one result was found, select it and continue without any prompts
                if (itemIdQueryResult.Count == 1)
                {
                    itemId = itemIdQueryResult[0].ID;
                }
            }

            return itemId;
        }

        // returns a list of strings representing the servers that should be parsed
        // can be either one server, in the case of the user requesting a specific server, or all servers in a datacenter
        private List<string> GetServer(string input, bool useDefaultWorld)
        {
            var resultsList = new List<string>();

            string server = null;

            // look to see if the input contains one of the server names
            foreach (var world in Enum.GetValues(typeof(Worlds)))
            {
                if (input.Contains(world.ToString()))
                {
                    server = world.ToString();
                }
            }

            // if user supplied a server, return that in the list
            if (server != null)
            {
                resultsList.Add(server);
                return resultsList;
            }

            // if we didn't find a server, but calling function requested we use the default world instead of the default datacenter,
            // return the default world instead of the default datacenter
            if (useDefaultWorld && server == null)
            {
                resultsList.Add(DefaultWorld.ToString());
                return resultsList;
            }

            // otherwise, add every server in the datacenter
            foreach (var world in Enum.GetValues(typeof(Worlds)))
                resultsList.Add(world.ToString());

            return resultsList;
        }

        // interactive user selection prompt - each item in the passed collection gets listed out with an emoji
        // user selects an emoji, and the handlecallback function is run with the corresponding item ID as its parameter
        // it's expected that this function will be the last call in a function before that terminates, and that the callback function
        // will re-run the function with the user-selected data
        // optional server parameter to preserve server filter option
        private async Task InteractiveUserSelectItem(List<ItemSearchResultModel> itemsList, InteractiveCommandReturn function, List<string> worldsToSearch)
        {
            string[] numbers = new[] { "0⃣", "1⃣", "2⃣", "3⃣", "4⃣", "5⃣", "6⃣", "7⃣", "8⃣", "9⃣", "🇦", "🇧", "🇨", "🇩", "🇪" };
            var numberEmojis = new List<Emoji>();

            EmbedBuilder embedBuilder = new EmbedBuilder();
            StringBuilder stringBuilder = new StringBuilder();

            // add the number of emojis we need to the emojis list, and build our string-list of search results
            for (int i = 0; i < itemsList.Count && i < numbers.Length; i++)
            {
                numberEmojis.Add(new Emoji(numbers[i]));
                // get key for this dictionaryentry at index
                var itemsDictionaryName = itemsList[i].Name;

                stringBuilder.AppendLine($"{numbers[i]} - {itemsDictionaryName}");
            }

            embedBuilder.WithDescription(stringBuilder.ToString());
            embedBuilder.WithColor(Color.Blue);

            // build a message and add reactions to it
            // reactions will be watched, and the one selected will fire the HandleFindTagReactionResult method, passing
            // that reaction's corresponding tagname and the function passed into this parameter
            var messageContents = new ReactionCallbackData("Did you mean... ", embedBuilder.Build());
            for (int i = 0; i < itemsList.Count; i++)
            {
                var counter = i;
                var itemId = itemsList[i].ID;
                messageContents.AddCallBack(numberEmojis[counter], async (c, r) => HandleInteractiveUserSelectCallback(itemId, function, worldsToSearch));
            }

            var message = await InlineReactionReplyAsync(messageContents);

            // add calling user and searchResults embed to a dict as a pair
            // this way we can hold multiple users' reaction messages and operate on them separately
            _dictFindItemUserEmbedPairs.Add(Context.User, message);
        }


        // this might get modified to accept a 'function' param that will run in a switch:case to
        // select what calling function this callback handler should re-run with the user-selected data
        // optional server parameter to preserve server filter option
        private async Task HandleInteractiveUserSelectCallback(int itemId, InteractiveCommandReturn function, List<string> worldsToSearch)
        {
            string searchLocation = "";

            // if user originally specified a world for the command, set it as the searchLocation to pass back to the command
            if (worldsToSearch.Intersect(Enum.GetNames(typeof(Worlds))).Count() == 1)
            {
                searchLocation = worldsToSearch.FirstOrDefault(x => Enum.GetNames(typeof(Worlds)).Contains(x));
            }

            // grab the calling user's pair of calling user & searchResults embed
            var dictEntry = _dictFindItemUserEmbedPairs.FirstOrDefault(x => x.Key == Context.User);

            // delete the calling user's searchResults embed, if it exists
            if (dictEntry.Key != null)
                await dictEntry.Value.DeleteAsync();

            switch (function)
            {
                case InteractiveCommandReturn.Price:
                    await MarketGetItemPrice($"{searchLocation} {itemId}");
                    break;
                case InteractiveCommandReturn.History:
                    await MarketGetItemHistory($"{searchLocation} {itemId}");
                    break;
                case InteractiveCommandReturn.Analyze:
                    await MarketAnalyzeItem($"{searchLocation} {itemId}");
                    break;
            }
        }

        //
        private string CleanCommandInput(string input)
        {
            var wordsToRemove = new List<string>();
            string result = input;

            // add each possible input into a list of words to look for
            foreach (var world in Enum.GetValues(typeof(Worlds)))
                wordsToRemove.Add(world.ToString());

            foreach (var word in wordsToRemove)
            {
                if (Regex.Match(input, $@"\b{word}\b", RegexOptions.IgnoreCase).Success)
                {
                    result = ReplaceWholeWord(input, word, "");
                }
            }

            return result;
        }

        //
        private async Task<bool> IsCompanionAPIUsable()
        {
            var isApiUp = await APIHeartbeatService.IsCompanionAPIUsable();

            if (isApiUp)
                return true;
            else
            {
                await InformUserOfAPIFailure();
                return false;
            }
        }

        //
        private async Task InformUserOfAPIFailure()
        {
            var apiStatus = APIHeartbeatService.ApiStatus;

            var humanReadableStatus = GetCustomAPIStatusFailureReason(apiStatus);

            await Context.Channel.SendMessageAsync(humanReadableStatus);
        }

        //
        private string GetCustomAPIStatusFailureReason(CustomApiStatus status)
        {
            string apiStatusHumanResponse = "";

            if (status == CustomApiStatus.NotLoggedIn)
                apiStatusHumanResponse = $"Not logged in to Companion API. Contact {Context.Guild.GetUser(110866678161645568).Mention}.";
            if (status == CustomApiStatus.UnderMaintenance)
                apiStatusHumanResponse = "SE's API is down for maintenance.";
            if (status == CustomApiStatus.AccessDenied)
                apiStatusHumanResponse = $"Access denied. Contact {Context.Guild.GetUser(110866678161645568).Mention}.";
            if (status == CustomApiStatus.ServiceUnavailable || status == CustomApiStatus.APIFailure)
                apiStatusHumanResponse = $"Something went wrong (API failure). Contact {Context.Guild.GetUser(110866678161645568).Mention}.";

            return apiStatusHumanResponse;
        }

        //
        private string ReplaceWholeWord(string original, string wordToFind, string replacement, RegexOptions regexOptions = RegexOptions.None)
        {
            string pattern = String.Format(@"\b{0}\b", wordToFind);
            string replaced = Regex.Replace(original, pattern, replacement, regexOptions).Trim(); // remove the unwanted word
            string ret = Regex.Replace(replaced, @"\s+", " "); // clear out any excess whitespace
            return ret;
        }
    }
}
