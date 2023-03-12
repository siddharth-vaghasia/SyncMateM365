using RestSharp;
using SyncMateM365.Interface;
using System.Text.Json;

namespace SyncMateM365.Services
{
    public class TokenService : ITokenService
    {
        private readonly string _clientid;
        private readonly string _clientsecret;
        private readonly ILogger<TokenService> _logger;
        public TokenService(ILogger<TokenService> logger, IConfiguration configuration)
        {
            _clientid = configuration.GetValue<string>("AzureAd:ClientId");
            _clientsecret = configuration.GetValue<string>("AzureAd:ClientSecret");
            _logger = logger;
        }
        public async Task<string> GetRefreshToken(string assertion)
        {
            try
            {
                var client = new RestClient("https://login.microsoftonline.com/common/oauth2/v2.0/token");
                client.Timeout = -1;
                var request = new RestRequest(Method.POST);
                request.AddHeader("Content-Type", "application/x-www-form-urlencoded");
                request.AddParameter("grant_type", "urn:ietf:params:oauth:grant-type:jwt-bearer");
                request.AddParameter("client_id", _clientid);
                request.AddParameter("client_secret", _clientsecret);
                request.AddParameter("assertion", assertion);
                request.AddParameter("scope", "Calendars.ReadWrite Calendars.Read offline_access");
                request.AddParameter("requested_token_use", "on_behalf_of");
                IRestResponse response = await client.ExecuteAsync(request);
                if (response.Content != null)
                {
                    string content = response.Content;
                    var a = JsonSerializer.Deserialize<TokenBody>(content);
                    if (a != null)
                    {
                        return a.refresh_token ?? "";
                    }
                    else
                    {
                        return "";
                    }
                }
                else
                {
                    return "";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(string.Format("Exception in TokenService {0}", ex));
                throw;
            }
        }

        public async Task<string> RenewToken(string refreshToken)
        {
            try
            {
                var client = new RestClient("https://login.microsoftonline.com/common/oauth2/v2.0/token");
                client.Timeout = -1;
                var request = new RestRequest(Method.POST);
                request.AddHeader("Content-Type", "application/x-www-form-urlencoded");
                request.AddParameter("grant_type", "refresh_token");
                request.AddParameter("client_id", _clientid);
                request.AddParameter("client_secret", _clientsecret);
                request.AddParameter("scope", "Calendars.ReadWrite Calendars.Read offline_access");
                request.AddParameter("refresh_token", refreshToken);
                IRestResponse response = await client.ExecuteAsync(request);
                if (response.Content != null)
                {
                    string content = response.Content;
                    var a = JsonSerializer.Deserialize<TokenBody>(content);
                    if (a != null)
                    {
                        return a.access_token ?? "";
                    }
                    else
                    {
                        return "";
                    }
                }
                else
                {
                    return "";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(string.Format("Exception in TokenService {0}", ex));
                throw;
            }

        }

        public class TokenBody
        {
            public string? token_type { get; set; }
            public string? scope { get; set; }
            public int? expires_in { get; set; }
            public int? ext_expires_in { get; set; }
            public string? access_token { get; set; }
            public string? refresh_token { get; set; }
        }
    }
}
