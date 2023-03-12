namespace SyncMateM365.Interface
{
    public interface ISubscribeEventService
    {
        public Task<string> CallSubscribeEventAPI(string? parentsubscription);
        public Task<string> DelteSubscribeEventAPI(string subscriptionid);
    }
}
