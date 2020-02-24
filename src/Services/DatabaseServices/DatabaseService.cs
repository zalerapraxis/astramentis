using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Astramentis.Services.DatabaseServiceComponents;
using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Astramentis.Services
{
    public class DatabaseService
    {
        public MongoClient _mongodb;
        public string _mongodbName;
        
        public DatabaseService(IConfigurationRoot config)
        {
            // assumes our db user auths to the same db as the one we're connecting to
            var username = config["dbUsername"];
            var password = config["dbPassword"];
            var host = config["dbHost"];
            var dbName = config["dbName"];

            _mongodb = new MongoClient($"mongodb://{username}:{password}@{host}/?authSource={dbName}");
            _mongodbName = dbName;
        }
    }
}
