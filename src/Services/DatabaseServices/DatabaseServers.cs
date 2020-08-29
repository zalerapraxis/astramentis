using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Astramentis.Models;
using Discord;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Astramentis.Services.DatabaseServiceComponents
{
    public class DatabaseServers
    {
        private readonly DatabaseService _databaseService;

        private MongoClient _mongodb;
        private string _mongodbName;

        public DatabaseServers(DatabaseService databaseService)
        {
            _databaseService = databaseService;

            _mongodb = _databaseService._mongodb;
            _mongodbName = _databaseService._mongodbName;
        }

        public async Task<List<DbDiscordServer>> GetServersInfo()
        {
            var database = _mongodb.GetDatabase(_mongodbName);
            var serverCollection = database.GetCollection<DbDiscordServer>("servers");

            var servers = await serverCollection.Find(new BsonDocument()).ToListAsync();

            return servers;
        }

        public async Task<bool> AddServerInfo(DbDiscordServer newServer)
        {
            var database = _mongodb.GetDatabase(_mongodbName);
            var serverCollection = database.GetCollection<DbDiscordServer>("servers");

            await serverCollection.InsertOneAsync(newServer);

            return true;
        }

        public async Task<bool> RemoveServerInfo(DbDiscordServer server)
        {
            var database = _mongodb.GetDatabase(_mongodbName);
            var serverCollection = database.GetCollection<DbDiscordServer>("servers");

            var filter = Builders<DbDiscordServer>.Filter.Eq("server_id", server.ServerId);

            await serverCollection.DeleteOneAsync(filter);

            return true;
        }

        public async Task<bool> EditServerInfo(string serverId, string key, dynamic value)
        {
            var database = _mongodb.GetDatabase(_mongodbName);
            var serverCollection = database.GetCollection<DbDiscordServer>("servers");

            var filter = Builders<DbDiscordServer>.Filter.Eq("server_id", serverId);
            // do we need to check if server info exists?

            // stage change
            var update = Builders<DbDiscordServer>.Update.Set(key, value);

            // commit change
            await serverCollection.UpdateOneAsync(filter, update);

            return true;
        }
    }
}
