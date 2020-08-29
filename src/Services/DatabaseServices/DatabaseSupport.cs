using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Astramentis.Models;
using Discord;
using Discord.Commands;
using MongoDB.Driver;

namespace Astramentis.Services.DatabaseServiceComponents
{
    public class DatabaseSupport
    {
        private readonly DatabaseService _databaseService;

        private MongoClient _mongodb;
        private string _mongodbName;

        public DatabaseSupport(DatabaseService databaseService)
        {
            _databaseService = databaseService;

            _mongodb = _databaseService._mongodb;
            _mongodbName = _databaseService._mongodbName;
        }

        // called via .tag add {name} {contents} command - returns true if add successful, false otherwise
        public async Task StoreSupportMessage(string message, SocketCommandContext context)
        {
            var newSupportMessage = new DbSupportMessage()
            {
                Message = message,
                MessageID = context.Message.Id.ToString(),
                AuthorID = context.User.Id.ToString(),
                ChannelID = context.Channel.Id.ToString(),
            };

            var database = _mongodb.GetDatabase(_mongodbName);
            var supportMessageCollection = database.GetCollection<DbSupportMessage>("supportMessages");

            await supportMessageCollection.InsertOneAsync(newSupportMessage);
        }

        public async Task RemoveSupportMessage(string messageID)
        {
            var database = _mongodb.GetDatabase(_mongodbName);
            var supportMessageCollection = database.GetCollection<DbSupportMessage>("supportMessages");

            var builder = Builders<DbSupportMessage>.Filter;
            FilterDefinition<DbSupportMessage> filter = builder.Eq("messageId", messageID);

            supportMessageCollection.DeleteOneAsync(filter);
        }

        public async Task<DbSupportMessage> GetSupportMessage(string messageID)
        {
            var database = _mongodb.GetDatabase(_mongodbName);
            var supportMessageCollection = database.GetCollection<DbSupportMessage>("supportMessages");

            var builder = Builders<DbSupportMessage>.Filter;
            FilterDefinition<DbSupportMessage> filter = builder.Eq("messageId", messageID);

            return supportMessageCollection.FindAsync(filter).Result.FirstOrDefault();
        }

    }
}
