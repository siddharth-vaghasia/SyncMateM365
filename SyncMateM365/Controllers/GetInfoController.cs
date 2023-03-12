using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Graph;
using Microsoft.Identity.Web;
using SyncMateM365.Interface;
using SyncMateM365.Models;
using SyncMateM365.Services;

namespace SyncMateM365.Controllers
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [ApiController]
    [Route("api/[controller]")]
    public class GetInfoController : ControllerBase
    {
        static readonly string[] scopesToAccessDownstreamApi = new string[] { "Calendars.Read", "Calendars.ReadWrite", "offline_access" };

        private readonly ILogger<GetInfoController> _logger;
        private readonly UserInfoService _userInfoService;
        private readonly UserMappingService _userMappingService;
        private readonly ITokenAcquisition _tokenAcquisition;
        private readonly GraphServiceClient _graphServiceClient;
        private readonly ITokenService _tokenService;
        private readonly IEventService _eventService;
        public GetInfoController(ILogger<GetInfoController> logger,UserInfoService userInfoService,
            UserMappingService userMappingService,
            ITokenAcquisition tokenAcquisition,
            GraphServiceClient graphServiceClient,
            ITokenService tokenHelper,
            IEventService eventService)
        {
            _logger = logger;
            _userInfoService = userInfoService;
            _userMappingService = userMappingService;
            _tokenAcquisition = tokenAcquisition;
            _graphServiceClient = graphServiceClient;
            _tokenService = tokenHelper;
            _eventService = eventService;
        }

        // [Authorize]
        [HttpPost("GetAllEvents")]
        public async Task<string> GetAllEvents()
        {
            try
            {
                //get the on behalf of token
                var result = await _tokenAcquisition.GetAuthenticationResultForUserAsync(scopes: scopesToAccessDownstreamApi);
                List<IUserEventsCollectionPage> allevents = new List<IUserEventsCollectionPage>();

                //check if the subscription already present
                var userInfopresent = await _userInfoService.GetByUserId(result.Account.HomeAccountId.ToString());
                if (userInfopresent != null)
                {
                    var userMappingValue = await _userMappingService.GetAsync(userInfopresent.SubscriptionId);
                    var events = await this._graphServiceClient.Me.Events.Request().GetAsync();
                    allevents.Add(events);

                    if (userMappingValue != null && !string.IsNullOrEmpty(userMappingValue.Id))
                    {
                        foreach (var item in userMappingValue.Mappings)
                        {
                            if (item != userInfopresent.SubscriptionId)
                            {
                                var userInfoValue = await _userInfoService.GetBySubscription(item);
                                if (userInfoValue != null && userInfoValue.RefreshToken != null)
                                {
                                    var userToken = await _tokenService.RenewToken(userInfoValue.RefreshToken);
                                    var newevent = this._eventService.Get(userToken);
                                    allevents.Add(newevent.Result);
                                }
                            }
                        }
                    }
                }
                return Newtonsoft.Json.JsonConvert.SerializeObject(new { status = "Success", message = allevents });
            }
            catch (Exception ex)
            {
                _logger.LogError(string.Format("Exception in GetInfoController {0}", ex));
                throw;
            }
        }

        [Authorize]
        [HttpGet("GetAllUsersInfo")]
        public async Task<List<UserInfo>> GetAllUsersInfo()
        {
            try
            {
                //get the on behalf of token
                var result = await _tokenAcquisition.GetAuthenticationResultForUserAsync(scopes: scopesToAccessDownstreamApi);

                List<UserInfo> alluserInfo = new List<UserInfo>();

                //check if the subscription already present
                var userInfopresent = await _userInfoService.GetByUserId(result.Account.HomeAccountId.ToString());
                if (userInfopresent != null)
                {
                    alluserInfo.Add(userInfopresent);
                    var userMappingValue = await _userMappingService.GetAsync(userInfopresent.SubscriptionId);
                    if (userMappingValue != null && !string.IsNullOrEmpty(userMappingValue.Id))
                    {
                        foreach (var item in userMappingValue.Mappings)
                        {
                            if (item != userInfopresent.SubscriptionId)
                            {
                                var userInfoValue = await _userInfoService.GetBySubscription(item);
                                if (userInfoValue != null)
                                {
                                    alluserInfo.Add(userInfoValue);
                                }
                            }
                        }
                    }
                }
                return alluserInfo;
            }
            catch (Exception ex)
            {
                _logger.LogError(string.Format("Exception in GetInfoController {0}", ex));
                throw;
            }
        }
    }
}
