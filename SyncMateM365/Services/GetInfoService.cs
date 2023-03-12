using Microsoft.Graph;
using Microsoft.Identity.Web;
using Newtonsoft.Json;
using SyncMateM365.Interface;
using SyncMateM365.Models;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace SyncMateM365.Services
{
    public static class GetInfoServiceExtensions
    {
        public static void AddGetInfo(this IServiceCollection services)
        {
            // https://docs.microsoft.com/en-us/dotnet/standard/microservices-architecture/implement-resilient-applications/use-httpclientfactory-to-implement-resilient-http-requests
            services.AddHttpClient<IGetInfoService, GetInfoService>();
        }
    }
    public class GetInfoService : IGetInfoService
    {
        private readonly HttpClient _httpClient;
        private readonly string _SubscribeEventBaseAddress = string.Empty;
        private readonly ITokenAcquisition _tokenAcquisition;
        private readonly ILogger<GetInfoService> _logger;
        public GetInfoService(ILogger<GetInfoService> logger,
            ITokenAcquisition tokenAcquisition, HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _tokenAcquisition = tokenAcquisition;
            _SubscribeEventBaseAddress = configuration.GetValue<string>("DownstreamApi:BaseUrl");
            _logger = logger;
        }

        public async Task<List<IUserEventsCollectionPage>?> GetAllEventAPI()
        {
            try
            {
                await PrepareAuthenticatedClient();
                string suburl = $"{_SubscribeEventBaseAddress}/GetInfo/GetAllEvents";

                var response = await _httpClient.GetAsync(suburl);
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    if (content != null)
                    {
                        return JsonConvert.DeserializeObject<List<IUserEventsCollectionPage>>(content);
                    }
                }

                return new List<IUserEventsCollectionPage>();
            }
            catch (Exception ex)
            {
                _logger.LogError(string.Format("Exception in GetInfoService {0}", ex));
                throw;
            }
        }

        public async Task<List<UserInfo>?> GetAllUsersInfo()
        {
            try
            {
                await PrepareAuthenticatedClient();
                string suburl = $"{_SubscribeEventBaseAddress}/GetInfo/GetAllUsersInfo";

                var response = await _httpClient.GetAsync(suburl);
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    if (content != null)
                    {
                        return JsonConvert.DeserializeObject<List<UserInfo>>(content);
                    }
                }

                return new List<UserInfo>();
            }
            catch (Exception ex)
            {
                _logger.LogError(string.Format("Exception in GetInfoService {0}", ex));
                throw;
            }
        }

        //Acquire a token and add it as Bearer to Authorization header
        private async Task PrepareAuthenticatedClient()
        {
            try
            {
                //new [] { "api://983aba2d-af8e-48f3-bab4-375186367c5e/access_as_user" }
                var accessToken = await _tokenAcquisition.GetAuthenticationResultForUserAsync(new[] { "api://983aba2d-af8e-48f3-bab4-375186367c5e/access_as_user", "Calendars.Read", "Calendars.ReadWrite" });
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken.AccessToken);
                _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            }
            catch (Exception ex)
            {
                _logger.LogError(string.Format("Exception in GetInfoService {0}", ex));
                throw;
            }
        }
    }
}
