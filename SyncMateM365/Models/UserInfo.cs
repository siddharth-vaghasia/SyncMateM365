using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace SyncMateM365.Models
{
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
