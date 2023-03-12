using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace SyncMateM365.Models
{
    public class MeetingMapping
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        public string ParentMeetingId { get; set; } = null!;
        public List<MeetingMappingItem> Mappings { get; set; } = null!;
    }

    public class MeetingMappingItem
    {
        public string SubscriptionId { get; set; } = null!;
        public string MeetingId { get; set; } = null!;
    }
}
