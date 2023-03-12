using Microsoft.Graph;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web;
using MongoDB.Bson.IO;
using SyncMateM365.Interface;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;

namespace SyncMateM365.Services
{
    public static class SubscribeEventServiceExtensions
    {
        public static void AddSubscribeEventService(this IServiceCollection services)
        {
            // https://docs.microsoft.com/en-us/dotnet/standard/microservices-architecture/implement-resilient-applications/use-httpclientfactory-to-implement-resilient-http-requests
            services.AddHttpClient<ISubscribeEventService, SubscribeEventService>();
        }
    }
    public class SubscribeEventService : ISubscribeEventService
    {
        private readonly HttpClient _httpClient;
        private readonly string _SubscribeEventBaseAddress = string.Empty;
        private readonly ITokenAcquisition _tokenAcquisition;
        private readonly ILogger<SubscribeEventService> _logger;
        public SubscribeEventService(ILogger<SubscribeEventService> logger, 
            ITokenAcquisition tokenAcquisition, HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _tokenAcquisition = tokenAcquisition;
            _SubscribeEventBaseAddress = configuration.GetValue<string>("DownstreamApi:BaseUrl");
            _logger = logger;
        }

        public async Task<string> CallSubscribeEventAPI(string? parentsubscription)
        {
            try
            {
                await PrepareAuthenticatedClient();
                string suburl = $"{_SubscribeEventBaseAddress}/SubscribeEvent";
                if (!string.IsNullOrEmpty(parentsubscription))
                {
                    suburl += "?" + parentsubscription.Replace('"', ' ').Trim();
                }
                var response = await _httpClient.GetAsync(suburl);
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    var content = await response.Content.ReadAsStringAsync();

                    return content;
                }

                return "";
            }
            catch (Exception ex)
            {
                _logger.LogError(string.Format("Exception in SubscribeEventService {0}", ex));
                throw;
            }
        }

        public async Task<string> DelteSubscribeEventAPI(string subscriptionid)
        {
            try
            {
                await PrepareAuthenticatedClient();
                string suburl = $"{_SubscribeEventBaseAddress}/SubscribeEvent";
                suburl += "?" + subscriptionid.Replace('"', ' ').Trim();

                var response = await _httpClient.PostAsync(suburl, null);
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    var content = await response.Content.ReadAsStringAsync();

                    return content;
                }

                return "";
            }
            catch (Exception ex)
            {
                _logger.LogError(string.Format("Exception in SubscribeEventService {0}", ex));
                throw;
            }
        }

        //Acquire a token and add it as Bearer to Authorization header
        private async Task PrepareAuthenticatedClient()
        {
            var scopes = new[] { "api://983aba2d-af8e-48f3-bab4-375186367c5e/access_as_user", "Calendars.Read", "Calendars.ReadWrite" };
            try
            {
                var accessToken = await _tokenAcquisition.GetAuthenticationResultForUserAsync(scopes);
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken.AccessToken);
                _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            }
            catch (MicrosoftIdentityWebChallengeUserException ex)
            {
                _tokenAcquisition.ReplyForbiddenWithWwwAuthenticateHeader(scopes, ex.MsalUiRequiredException);
            }
            catch (MsalUiRequiredException ex)
            {
                _tokenAcquisition.ReplyForbiddenWithWwwAuthenticateHeader(scopes, ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(string.Format("Exception in SubscribeEventService {0}", ex));
                throw;
            }
        }
    }
}
