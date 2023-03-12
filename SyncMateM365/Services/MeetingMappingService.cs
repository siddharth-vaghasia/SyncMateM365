

using Microsoft.Extensions.Options;
using MongoDB.Driver;
using SyncMateM365.Models;

namespace SyncMateM365.Services
{
    public class MeetingMappingService
    {
        private readonly IMongoCollection<MeetingMapping> _meetingInfoCollection;
        private readonly ILogger<MeetingMappingService> _logger;

        public MeetingMappingService(ILogger<MeetingMappingService> logger,
            IOptions<UserInfoDatabaseSettings> userInfoDatabaseSettings)
        {
            var mongoClient = new MongoClient(
                userInfoDatabaseSettings.Value.ConnectionString);

            var mongoDatabase = mongoClient.GetDatabase(
                userInfoDatabaseSettings.Value.DatabaseName);

            _meetingInfoCollection = mongoDatabase.GetCollection<MeetingMapping>(
                userInfoDatabaseSettings.Value.MeetingMappingCollectionName);
            _logger = logger;
        }

        public async Task<List<MeetingMapping>> GetAsync()
        {
            try
            {
                return await _meetingInfoCollection.Find(_ => true).ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(string.Format("Exception in MeetingMapping {0}", ex));
                throw;
            }
        }

        public async Task<MeetingMapping?> GetAsync(string id)
        {
            try
            {
                var filter = Builders<MeetingMapping>.Filter.Or(
                    Builders<MeetingMapping>.Filter.Eq(x => x.ParentMeetingId, id),
                    Builders<MeetingMapping>.Filter.ElemMatch(x => x.Mappings, m => m.MeetingId == id)
                );
                return await _meetingInfoCollection.Find(filter).FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(string.Format("Exception in MeetingMapping {0}", ex));
                throw;
            }
        }

        public async Task CreateAsync(MeetingMapping newBook)
        {
            try
            {
                await _meetingInfoCollection.InsertOneAsync(newBook);
            }
            catch (Exception ex)
            {
                _logger.LogError(string.Format("Exception in MeetingMapping {0}", ex));
                throw;
            }
        }

        public async Task UpdateAsync(string id, MeetingMapping updatedBook)
        {
            try
            {
                await _meetingInfoCollection.ReplaceOneAsync(x => x.Id == id, updatedBook);
            }
            catch (Exception ex)
            {
                _logger.LogError(string.Format("Exception in MeetingMapping {0}", ex));
                throw;
            }
        }

        public async Task RemoveAsync(string id)
        {
            try
            {
                await _meetingInfoCollection.DeleteOneAsync(x => x.ParentMeetingId == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(string.Format("Exception in MeetingMapping {0}", ex));
                throw;
            }
        }
    }
}
