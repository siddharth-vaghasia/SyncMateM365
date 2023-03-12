using Azure.Core;
using Microsoft.Graph;
using SyncMateM365.Interface;
using SyncMateM365.Models;

namespace SyncMateM365.Services
{
    public class EventService : IEventService
    {
        private readonly ILogger<EventService> _logger;
        public EventService(ILogger<EventService> logger) 
        { 
            _logger= logger;
        }
        public async Task<IUserEventsCollectionPage> Get(string token)
        {
            try
            {
                var temp = new GraphServiceClient(new BearerTokenCredential(token));
                var events = await temp.Me.Events.Request().GetAsync();
                return events;
            }
            catch (Exception ex)
            {
                _logger.LogError(string.Format("Exception in EventService {0}", ex));
                throw;
            }
        }
        public async Task<Event?> GetEventInfo(SubscriptionData subscriptionInfo, string token)
        {
            try { 
            if (subscriptionInfo != null && subscriptionInfo.value.Count > 0)
            {
                var temp = new GraphServiceClient(new BearerTokenCredential(token));
                return await temp.Me.Events[subscriptionInfo.value[0].resource.Split("/").Last()].Request().GetAsync();
            }
            return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(string.Format("Exception in EventService {0}", ex));
                throw;
            }
        }
        public async Task<Event?> AddEventInfo(Event eventinfo, string token)
        {
            try 
            {
                var allevents = await Get(token);
                var similarevent = allevents.FirstOrDefault(t=>t.Start == eventinfo.Start && t.End == eventinfo.End && t.Subject == "Not Available");
                if (eventinfo != null && similarevent == null)
                {
                    var tempEvent = new Event();
                    tempEvent.Start = eventinfo.Start;
                    tempEvent.End = eventinfo.End;
                    tempEvent.Subject = "Not Available";
                    tempEvent.IsAllDay = eventinfo.IsAllDay;
                    tempEvent.IsDraft = eventinfo.IsDraft;
                    tempEvent.IsCancelled = eventinfo.IsCancelled;
                    var temp = new GraphServiceClient(new BearerTokenCredential(token));
                    return await temp.Me.Events.Request().AddAsync(tempEvent);
                }
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(string.Format("Exception in EventService {0}", ex));
                throw;
            }
        }
        public async Task<Event?> UpdateEventInfo(string id, Event eventinfo, string token)
        {
            try
            {
                if (eventinfo != null)
                {
                    var tempEvent = new Event();
                    tempEvent.Start = eventinfo.Start;
                    tempEvent.End = eventinfo.End;
                    tempEvent.Subject = "Not Available";
                    tempEvent.IsAllDay = eventinfo.IsAllDay;
                    tempEvent.IsDraft = eventinfo.IsDraft;
                    tempEvent.IsCancelled = eventinfo.IsCancelled;

                    var temp = new GraphServiceClient(new BearerTokenCredential(token));
                    return await temp.Me.Events[id].Request().UpdateAsync(tempEvent);
                }
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(string.Format("Exception in EventService {0}", ex));
                throw;
            }
}
        public async Task DeleteEventInfo(string id, string token)
        {
            try
            {
                var temp = new GraphServiceClient(new BearerTokenCredential(token));
                await temp.Me.Events[id].Request().DeleteAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(string.Format("Exception in EventService {0}", ex));
                throw;
            }
        }
    }

    public class BearerTokenCredential : TokenCredential
    {
        private readonly string _bearerToken;

        public BearerTokenCredential(string bearerToken)
        {
            _bearerToken = bearerToken;
        }

        public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken)
        {
            return new AccessToken(_bearerToken, DateTimeOffset.MaxValue);
        }

        public override ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken)
        {
            return new ValueTask<AccessToken>(GetToken(requestContext, cancellationToken));
        }
    }
}
