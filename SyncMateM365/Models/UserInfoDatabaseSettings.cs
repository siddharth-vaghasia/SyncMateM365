namespace SyncMateM365.Models
{
    public class UserInfoDatabaseSettings
    {
        public string ConnectionString { get; set; } = null!;
        public string DatabaseName { get; set; } = null!;
        public string UserInfoCollectionName { get; set; } = null!;
        public string UserMappingCollectionName { get; set; } = null!;
        public string MeetingMappingCollectionName { get; set; } = null!;
    }
}
