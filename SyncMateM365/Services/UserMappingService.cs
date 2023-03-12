using Microsoft.Extensions.Options;
using MongoDB.Driver;
using SyncMateM365.Models;

namespace SyncMateM365.Services
{
    public class UserMappingService
    {
        private readonly IMongoCollection<UserMapping> _userInfoCollection;
        private readonly ILogger<UserMappingService> _logger;

        public UserMappingService(ILogger<UserMappingService> logger,
            IOptions<UserInfoDatabaseSettings> userInfoDatabaseSettings)
        {
            var mongoClient = new MongoClient(
                userInfoDatabaseSettings.Value.ConnectionString);

            var mongoDatabase = mongoClient.GetDatabase(
                userInfoDatabaseSettings.Value.DatabaseName);

            _userInfoCollection = mongoDatabase.GetCollection<UserMapping>(
                userInfoDatabaseSettings.Value.UserMappingCollectionName);

            _logger = logger;
        }

        public async Task<List<UserMapping>> GetAsync()
        {
            try
            {
                return await _userInfoCollection.Find(_ => true).ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(string.Format("Exception in UserMappingService {0}", ex));
                throw;
            }
        }

        public async Task<UserMapping?> GetAsync(string id)
        {
            try
            {
                var filter = Builders<UserMapping>.Filter.AnyEq(x => x.Mappings, id);
                return await _userInfoCollection.Find(filter).FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(string.Format("Exception in UserMappingService {0}", ex));
                throw;
            }
        }

        public async Task CreateAsync(UserMapping newBook)
        {
            try
            {
                await _userInfoCollection.InsertOneAsync(newBook);
            }
            catch (Exception ex)
            {
                _logger.LogError(string.Format("Exception in UserMappingService {0}", ex));
                throw;
            }
        }

        public async Task UpdateAsync(string id, UserMapping updatedBook)
        {
            try
            {
                await _userInfoCollection.ReplaceOneAsync(x => x.Id == id, updatedBook);
            }
            catch (Exception ex)
            {
                _logger.LogError(string.Format("Exception in UserMappingService {0}", ex));
                throw;
            }
        }

        public async Task RemoveAsync(string id)
        {
            try
            {
                await _userInfoCollection.DeleteOneAsync(x => x.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(string.Format("Exception in UserMappingService {0}", ex));
                throw;
            }
        }
    }
}
