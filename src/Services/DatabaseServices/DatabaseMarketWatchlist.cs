﻿using System;
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

        public async Task<List<DbWatchlistEntry>> GetWatchlist()
        {
            var database = _mongodb.GetDatabase(_mongodbName);
            var watchlistCollection = database.GetCollection<DbWatchlistEntry>("watchlist");

            var watchlist = await watchlistCollection.Find(new BsonDocument()).ToListAsync();

            return watchlist;
        }

        public async Task<bool> AddToWatchlist(DbWatchlistEntry entry)
        {
            var database = _mongodb.GetDatabase(_mongodbName);
            var watchlistCollection = database.GetCollection<DbWatchlistEntry>("watchlist");

            await watchlistCollection.InsertOneAsync(entry);

            return true;
        }

        public async Task<bool> RemoveFromWatchlist(DbWatchlistEntry entry)
        {
            var database = _mongodb.GetDatabase(_mongodbName);
            var watchlistCollection = database.GetCollection<DbWatchlistEntry>("watchlist");

            FilterDefinition<DbWatchlistEntry> filter;
            var builder = Builders<DbWatchlistEntry>.Filter;

            filter = builder.Eq("itemname", entry.ItemName);

            var exists = watchlistCollection.FindAsync(filter).Result.Any();

            if (exists)
                await watchlistCollection.DeleteOneAsync(filter);

            return true;
        }
    }
}
