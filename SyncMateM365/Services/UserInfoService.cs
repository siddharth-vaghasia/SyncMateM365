using Microsoft.Extensions.Options;
using MongoDB.Driver;
using SyncMateM365.Models;

namespace SyncMateM365.Services
{
    public class UserInfoService
    {
        private readonly IMongoCollection<UserInfo> _userInfoCollection;
        private readonly ILogger<UserInfoService> _logger;

        public UserInfoService(ILogger<UserInfoService> logger,
            IOptions<UserInfoDatabaseSettings> userInfoDatabaseSettings)
        {
            var mongoClient = new MongoClient(
                userInfoDatabaseSettings.Value.ConnectionString);

            var mongoDatabase = mongoClient.GetDatabase(
                userInfoDatabaseSettings.Value.DatabaseName);

            _userInfoCollection = mongoDatabase.GetCollection<UserInfo>(
                userInfoDatabaseSettings.Value.UserInfoCollectionName);

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
}
