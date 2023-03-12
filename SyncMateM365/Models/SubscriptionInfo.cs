namespace SyncMateM365.Models
{
    public class SubscriptionInfo
    {
        public string subscriptionId { get; set; } = null!;
        public DateTime subscriptionExpirationDateTime { get; set; }
        public string changeType { get; set; } = null!;
        public string resource { get; set; } = null!;
        public string clientState { get; set; } = null!;
        public string tenantId { get; set; } = null!;
    }

    public class SubscriptionData
    {
        public List<SubscriptionInfo> value { get; set; } = null!;
    }
}
