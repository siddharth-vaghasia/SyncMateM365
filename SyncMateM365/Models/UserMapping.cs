using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace SyncMateM365.Models
{
    public class UserMapping
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }
        public List<string> Mappings { get; set; } = null!;
    }
}
