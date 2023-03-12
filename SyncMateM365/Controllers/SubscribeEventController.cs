using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Graph;
using Microsoft.Identity.Web;
using MongoDB.Bson;
using SyncMateM365.Interface;
using SyncMateM365.Models;
using SyncMateM365.Services;

namespace SyncMateM365.Controllers
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [ApiController]
    [Route("api/[controller]")]
    public class SubscribeEventController : ControllerBase
    {
        static readonly string[] scopesToAccessDownstreamApi = new string[] { "Calendars.Read", "Calendars.ReadWrite", "offline_access" };

        private readonly ILogger<SubscribeEventController> _logger;
        private readonly ITokenAcquisition _tokenAcquisition;
        private readonly GraphServiceClient _graphServiceClient;
        private readonly ITokenService _tokenService;
        private readonly UserInfoService _userInfoService;
        private readonly UserMappingService _userMappingService;
        private readonly string _hostedDomain;

        public SubscribeEventController(ILogger<SubscribeEventController> logger,
            ITokenAcquisition tokenAcquisition, GraphServiceClient graphServiceClient,
            ITokenService tokenHelper, UserInfoService userInfoService,
            UserMappingService userMappingService, IConfiguration configuration)
        {
            _logger = logger;
            _tokenAcquisition = tokenAcquisition;
            _graphServiceClient = graphServiceClient;
            _tokenService = tokenHelper;
            _userInfoService = userInfoService;
            _userMappingService = userMappingService;
            _hostedDomain = configuration.GetValue<string>("HostedDomain");
        }
        [Authorize]
        [HttpGet]
        public async Task<string> Get()
        {
            try
            {
                //get the on behalf of token
                var result = await _tokenAcquisition.GetAuthenticationResultForUserAsync(scopes: scopesToAccessDownstreamApi);

                //check if the subscription already present
                var userInfopresent = await _userInfoService.GetByUserId(result.Account.HomeAccountId.ToString());
                if (userInfopresent != null)
                {
                    return userInfopresent.SubscriptionId;
                }
                else
                {
                    var queryString = HttpContext.Request.QueryString.ToString();

                    //Add Subscription
                    var subscription = new Subscription();
                    subscription.ChangeType = "created,updated,deleted";
                    subscription.NotificationUrl = String.Format("https://{0}/api/webhook", _hostedDomain);
                    subscription.Resource = "me/events";
                    subscription.ClientState = "secretClientValue";
                    DateTimeOffset offset = DateTimeOffset.Now;
                    subscription.ExpirationDateTime = offset.AddDays(2);
                    var addedsubscription = await this._graphServiceClient.Subscriptions.Request().AddAsync(subscription);

                    //Get refreshToken
                    var refreshToken = await _tokenService.GetRefreshToken(HttpContext.Request.Headers.Authorization.ToString().Substring(7));

                    //Add User Info to database
                    var userInfo = new UserInfo();
                    userInfo.Id = ObjectId.GenerateNewId().ToString();
                    userInfo.UserId = result.Account.HomeAccountId.ToString();
                    userInfo.UserPrincipalName = result.Account.Username.ToString();
                    userInfo.RefreshToken = refreshToken;
                    userInfo.SubscriptionId = addedsubscription.Id;

                    await _userInfoService.CreateAsync(userInfo);

                    if (string.IsNullOrEmpty(queryString))
                    {
                        var userMapping = new UserMapping();
                        userMapping.Id = ObjectId.GenerateNewId().ToString();
                        userMapping.Mappings = new List<string> { addedsubscription.Id };
                        await _userMappingService.CreateAsync(userMapping);
                    }
                    else
                    {
                        var userMappingValue = await _userMappingService.GetAsync(queryString.Substring(1));
                        if (userMappingValue != null && !string.IsNullOrEmpty(userMappingValue.Id))
                        {
                            userMappingValue.Mappings.Add(addedsubscription.Id);
                            await _userMappingService.UpdateAsync(userMappingValue.Id, userMappingValue);
                        }
                    }

                    return addedsubscription.Id;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(string.Format("Exception in SubscribeEventController {0}", ex));
                throw;
            }
        }

        [Authorize]
        [HttpPost]
        public async Task Delete()
        {
            try
            {
                await _tokenAcquisition.GetAuthenticationResultForUserAsync(scopes: scopesToAccessDownstreamApi);
                var queryString = HttpContext.Request.QueryString.ToString();
                if (!string.IsNullOrEmpty(queryString))
                {
                    var subscriptionid = queryString.Substring(1);

                    var userInfoValue = await _userInfoService.GetBySubscription(subscriptionid);
                    if (userInfoValue != null && userInfoValue.RefreshToken != null)
                    {
                        var userToken = await _tokenService.RenewToken(userInfoValue.RefreshToken);
                        var temp = new GraphServiceClient(new BearerTokenCredential(userToken));
                        try
                        {
                            await temp.Subscriptions[subscriptionid].Request().DeleteAsync();
                        }
                        catch (Exception)
                        {
                            _logger.LogError($"Failed to delete subscription with {subscriptionid}");
                        }
                        finally
                        {
                            await this._userInfoService.RemoveAsync(subscriptionid);

                            var userMapping = await this._userMappingService.GetAsync(subscriptionid);
                            if (userMapping != null)
                            {
                                userMapping.Mappings.Remove(subscriptionid);
                                await this._userMappingService.UpdateAsync(subscriptionid, userMapping);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(string.Format("Exception in SubscribeEventController {0}", ex));
                throw;
            }
        }
    }
}