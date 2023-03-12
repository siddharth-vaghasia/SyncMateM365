using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncMateM365Scheduler
{

    public class UserInfoService
    {
        private readonly IMongoCollection<UserInfo> _userInfoCollection;
        private readonly ILogger _logger;

        public UserInfoService(ILogger logger,
            string connectionString, string databaseName, string userInfoCollectionName)
        {
            var mongoClient = new MongoClient(connectionString);

            var mongoDatabase = mongoClient.GetDatabase(databaseName);

            _userInfoCollection = mongoDatabase.GetCollection<UserInfo>(userInfoCollectionName);

            _logger = logger;
        }

        public async Task<List<UserInfo>> GetAsync()
        {
            try
            {
                return await _userInfoCollection.Find(_ => true).ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(string.Format("Exception in UserInfoService {0}", ex));
                throw;
            }
        }

        public async Task<UserInfo?> GetAsync(string id)
        {
            try
            {
                return await _userInfoCollection.Find(x => x.Id == id).FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(string.Format("Exception in UserInfoService {0}", ex));
                throw;
            }
        }

        public async Task<UserInfo?> GetBySubscription(string subscriptionId)
        {
            try
            {
                return await _userInfoCollection.Find(x => x.SubscriptionId == subscriptionId).FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(string.Format("Exception in UserInfoService {0}", ex));
                throw;
            }
        }

        public async Task<UserInfo?> GetByUserId(string userId)
        {
            try
            {
                return await _userInfoCollection.Find(x => x.UserId == userId).FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(string.Format("Exception in UserInfoService {0}", ex));
                throw;
            }
        }

        public async Task CreateAsync(UserInfo newBook)
        {
            try
            {
                await _userInfoCollection.InsertOneAsync(newBook);
            }
            catch (Exception ex)
            {
                _logger.LogError(string.Format("Exception in UserInfoService {0}", ex));
                throw;
            }
        }

        public async Task UpdateAsync(string id, UserInfo updatedBook)
        {
            try
            {
                await _userInfoCollection.ReplaceOneAsync(x => x.SubscriptionId == id, updatedBook);
            }
            catch (Exception ex)
            {
                _logger.LogError(string.Format("Exception in UserInfoService {0}", ex));
                throw;
            }
        }

        public async Task RemoveAsync(string id)
        {
            try
            {
                await _userInfoCollection.DeleteOneAsync(x => x.SubscriptionId == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(string.Format("Exception in UserInfoService {0}", ex));
                throw;
            }
        }
    }

    public class UserInfo
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }
        public string SubscriptionId { get; set; } = null!;
        public string RefreshToken { get; set; } = null!;
        public string UserPrincipalName { get; set; } = null!;
        public string UserId { get; set; } = null!;

        public string BackgroundColor { get; set; }

    }
}
