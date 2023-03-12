using Microsoft.Graph;
using SyncMateM365.Models;

namespace SyncMateM365.Interface
{
    public interface IEventService
    {
        public Task<IUserEventsCollectionPage> Get(string token);
        public Task<Event?> GetEventInfo(SubscriptionData subscriptionInfo, string token);
        public Task<Event?> AddEventInfo(Event eventinfo, string token);
        public Task<Event?> UpdateEventInfo(string id, Event eventinfo, string token);
        public Task DeleteEventInfo(string id, string token);
    }
}
