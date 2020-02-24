using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Astramentis.Services.DatabaseServices
{
    public class DatabaseMarketWatchlist
    {
        private readonly DatabaseService _databaseService;

        private MongoClient _mongodb;
        private string _mongodbName;

        public DatabaseMarketWatchlist(DatabaseService databaseService)
        {
            _databaseService = databaseService;

            _mongodb = _databaseService._mongodb;
            _mongodbName = _databaseService._mongodbName;
        }

        public async Task<List<WatchlistEntry>> GetWatchlist()
        {
            var database = _mongodb.GetDatabase(_mongodbName);
            var watchlistCollection = database.GetCollection<WatchlistEntry>("watchlist");

            var watchlist = await watchlistCollection.Find(new BsonDocument()).ToListAsync();

            return watchlist;
        }

        public async Task<bool> AddToWatchlist(WatchlistEntry entry)
        {
            var database = _mongodb.GetDatabase(_mongodbName);
            var watchlistCollection = database.GetCollection<WatchlistEntry>("watchlist");

            await watchlistCollection.InsertOneAsync(entry);

            return true;
        }
    }
}
