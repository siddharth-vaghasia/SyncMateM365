using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Graph;
using MongoDB.Bson;
using SyncMateM365.Interface;
using SyncMateM365.Models;
using SyncMateM365.Services;
using System.Text.Json;
using System.Web;

namespace SyncMateM365.Controllers
{
    [AllowAnonymous]
    [ApiController]
    [Route("api/[controller]")]
    public class WebHookController : ControllerBase
    {
        private readonly ILogger<WebHookController> _logger;
        private readonly UserInfoService _userInfoService;
        private readonly ITokenService _tokenService;
        private readonly string _hostedDomain;
        private readonly UserMappingService _userMappingService;
        private readonly IEventService _eventService;
        private readonly MeetingMappingService _meetingMappingService;

        public WebHookController(ILogger<WebHookController> logger, UserInfoService userInfoService, ITokenService tokenHelper,
            IConfiguration configuration, UserMappingService userMappingService, IEventService eventService,
            MeetingMappingService meetingMappingService)
        {
            _logger = logger;
            _userInfoService = userInfoService;
            _tokenService = tokenHelper;
            _hostedDomain = configuration.GetValue<string>("HostedDomain");
            _userMappingService = userMappingService;
            _eventService = eventService;
            _meetingMappingService = meetingMappingService;
        }
        [HttpPost]
        public async Task<string> Post()
        {
            try
            {
                //For subscribing to the webhook
                var temp = HttpContext.Request.QueryString.ToString();
                if (temp.Length > 0)
                {
                    if (temp.Contains("?validationToken="))
                    {
                        return HttpUtility.UrlDecode(temp.Substring(17));
                    }
                    else
                    {
                        return "";
                    }
                }
                else
                {
                    // when actual webhook is triggered
                    var body = await ReadRequestBody(HttpContext);
                    var bodyData = JsonSerializer.Deserialize<SubscriptionData>(body);
                    if (bodyData != null)
                    {
                        if (bodyData.value.Count > 0)
                        {
                            if (bodyData.value[0].clientState == "secretClientValue")
                            {
                                switch (bodyData.value[0].changeType)
                                {
                                    case "created":
                                        CreateEventToSync(bodyData, body);
                                        break;
                                    case "updated":
                                        UpdateEventToSync(bodyData, body);
                                        break;
                                    case "deleted":
                                        DeleteEventToSync(bodyData, body);
                                        break;
                                    default:
                                        break;
                                }
                            }
                        }
                    }
                    return "";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(string.Format("Exception in WebHookController {0}", ex));
                throw;
            }
        }
        protected async void DeleteEventToSync(SubscriptionData bodyData, string body)
        {
            try
            {
                var isMeetingPresent = await CheckIfMeetingPresent(bodyData, true);
                if (isMeetingPresent.Item1)
                {
                    var meetingId = bodyData.value[0].resource.Split("/").Last();

                    var meetingmapping = await this._meetingMappingService.GetAsync(meetingId);
                    var userMappingValue = await _userMappingService.GetAsync(bodyData.value[0].subscriptionId);
                    if (userMappingValue != null && !string.IsNullOrEmpty(userMappingValue.Id) && meetingmapping != null)
                    {
                        foreach (var item in userMappingValue.Mappings)
                        {
                            if (item != bodyData.value[0].subscriptionId)
                            {
                                var fetchedMeeting = meetingmapping.Mappings.Find(t => t.SubscriptionId == item);
                                if (fetchedMeeting != null && fetchedMeeting.MeetingId != null)
                                {
                                    await RemoveEventToSync(fetchedMeeting.SubscriptionId, fetchedMeeting.MeetingId);
                                }
                            }
                        }
                        await _meetingMappingService.RemoveAsync(meetingId);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(string.Format("Exception in WebHookController {0}", ex));
                throw;
            }
        }
        protected async void UpdateEventToSync(SubscriptionData bodyData, string body)
        {
            try
            {
                var isMeetingPresent = await CheckIfMeetingPresent(bodyData, false);
                if (isMeetingPresent.Item1 && isMeetingPresent.Item2 != null)
                {
                    var meetingId = bodyData.value[0].resource.Split("/").Last();

                    var meetingmapping = await this._meetingMappingService.GetAsync(meetingId);

                    var userMappingValue = await _userMappingService.GetAsync(bodyData.value[0].subscriptionId);
                    if (userMappingValue != null && !string.IsNullOrEmpty(userMappingValue.Id) && meetingmapping != null)
                    {
                        foreach (var item in userMappingValue.Mappings)
                        {
                            if (item != bodyData.value[0].subscriptionId)
                            {
                                var fetchedMeeting = meetingmapping.Mappings.Find(t => t.SubscriptionId == item);
                                if (fetchedMeeting != null && fetchedMeeting.MeetingId != null)
                                {
                                    await PatchEventToSync(item, isMeetingPresent.Item2, fetchedMeeting.MeetingId);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(string.Format("Exception in WebHookController {0}", ex));
                throw;
            }
        }

        protected async void CreateEventToSync(SubscriptionData bodyData, string body)
        {
            try
            {
                var isMeetingPresent = await CheckIfMeetingPresent(bodyData, false);
                if (!isMeetingPresent.Item1 && isMeetingPresent.Item2 != null)
                {
                    var meetingId = bodyData.value[0].resource.Split("/").Last();
                    var meetingMapping = new MeetingMapping();
                    meetingMapping.Id = ObjectId.GenerateNewId().ToString();
                    meetingMapping.ParentMeetingId = meetingId;
                    meetingMapping.Mappings = new List<MeetingMappingItem>();

                    var userMappingValue = await _userMappingService.GetAsync(bodyData.value[0].subscriptionId);
                    if (userMappingValue != null && !string.IsNullOrEmpty(userMappingValue.Id))
                    {
                        foreach (var item in userMappingValue.Mappings)
                        {
                            if (item != bodyData.value[0].subscriptionId)
                            {
                                var neweventcreated = await PostEventToSync(item, isMeetingPresent.Item2);
                                var meetingMappingItem = new MeetingMappingItem();
                                if (neweventcreated != null)
                                {
                                    meetingMappingItem.MeetingId = neweventcreated.Id;
                                    meetingMappingItem.SubscriptionId = item;
                                    meetingMapping.Mappings.Add(meetingMappingItem);
                                }
                            }
                        }
                    }
                    await _meetingMappingService.CreateAsync(meetingMapping);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(string.Format("Exception in WebHookController {0}", ex));
                throw;
            }
        }

        protected async Task<Tuple<Boolean,Event?>> CheckIfMeetingPresent(SubscriptionData bodyData, Boolean isDelete)
        {
            try
            {
                var userInfoValue = await _userInfoService.GetBySubscription(bodyData.value[0].subscriptionId);
                if (userInfoValue != null)
                {
                    if (userInfoValue.RefreshToken != null)
                    {
                        var userToken = await _tokenService.RenewToken(userInfoValue.RefreshToken);
                        if (!isDelete)
                        {
                            var fetchedevent = await this._eventService.GetEventInfo(bodyData, userToken);
                            if (fetchedevent != null && fetchedevent.Subject != "Not Available")
                            {
                                var meetingId = bodyData.value[0].resource.Split("/").Last();

                                var ismeetingpresent = await this._meetingMappingService.GetAsync(meetingId);
                                if (ismeetingpresent != null)
                                {
                                    return new Tuple<Boolean, Event?>(true, fetchedevent);
                                }
                                else
                                {
                                    return new Tuple<Boolean, Event?>(false, fetchedevent);
                                }
                            }
                        }
                        else
                        {
                            var meetingId = bodyData.value[0].resource.Split("/").Last();

                            var ismeetingpresent = await this._meetingMappingService.GetAsync(meetingId);
                            if (ismeetingpresent != null)
                            {
                                return new Tuple<Boolean, Event?>(true, null);
                            }
                            else
                            {
                                return new Tuple<Boolean, Event?>(false, null);
                            }
                        }
                    }
                }
                return new Tuple<Boolean, Event?>(false, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(string.Format("Exception in WebHookController {0}", ex));
                throw;
            }
        }

        protected async Task<Event?> PostEventToSync(string subscriptionId, Event body)
        {
            try
            {
                var userInfoValue = await _userInfoService.GetBySubscription(subscriptionId);
                if (userInfoValue != null)
                {
                    if (userInfoValue.RefreshToken != null)
                    {
                        var userToken = await _tokenService.RenewToken(userInfoValue.RefreshToken);

                        var newevent = await this._eventService.AddEventInfo(body, userToken);
                        if (newevent != null)
                        {
                            return newevent;
                        }
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(string.Format("Exception in WebHookController {0}", ex));
                throw;
            }
        }

        protected async Task PatchEventToSync(string subscriptionId, Event body, string meetingid)
        {
            try
            {
                var userInfoValue = await _userInfoService.GetBySubscription(subscriptionId);
                if (userInfoValue != null)
                {
                    if (userInfoValue.RefreshToken != null)
                    {
                        var userToken = await _tokenService.RenewToken(userInfoValue.RefreshToken);
                        await this._eventService.UpdateEventInfo(meetingid, body, userToken);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(string.Format("Exception in WebHookController {0}", ex));
                throw;
            }
        }

        protected async Task RemoveEventToSync(string subscriptionId, string meetingid)
        {
            try
            {
                var userInfoValue = await _userInfoService.GetBySubscription(subscriptionId);
                if (userInfoValue != null)
                {
                    if (userInfoValue.RefreshToken != null)
                    {
                        var userToken = await _tokenService.RenewToken(userInfoValue.RefreshToken);
                        await this._eventService.DeleteEventInfo(meetingid, userToken);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(string.Format("Exception in WebHookController {0}", ex));
                throw;
            }
        }

        public static async Task<string> ReadRequestBody(HttpContext context)
        {
            using var reader = new StreamReader(context.Request.Body);
            return await reader.ReadToEndAsync();
        }
    }
}
