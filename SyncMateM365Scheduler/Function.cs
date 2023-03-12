using Azure.Core;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Newtonsoft.Json.Linq;
using System.Threading;

namespace SyncMateM365Scheduler
{
    public class Functions
    {
        [Singleton]
        [NoAutomaticTrigger]
        [FunctionName("SyncMateM365Scheduler")]
        public static async Task Run(ILogger logger)
        {
            try
            {
                logger.LogInformation("Sync Mate CRON job started");
                var configurationBuilder = new ConfigurationBuilder()
                    .SetBasePath(Environment.CurrentDirectory)
                    .AddJsonFile("appsettings.json");

                var configuration = configurationBuilder.Build();

                var dbconnectionstring = configuration.GetValue<string>("UserInfoDatabase:ConnectionString");
                var databaseName = configuration.GetValue<string>("UserInfoDatabase:DatabaseName");
                var userInfoCollectionName = configuration.GetValue<string>("UserInfoDatabase:UserInfoCollectionName");
                var clientid = configuration.GetValue<string>("AzureAd:ClientId");
                var clientSecret = configuration.GetValue<string>("AzureAd:ClientSecret");

                var userinfoservice = new UserInfoService(logger, dbconnectionstring, databaseName, userInfoCollectionName);

                var tokenservice = new TokenService(logger, clientid, clientSecret);

                var allusers = await userinfoservice.GetAsync();

                foreach (var item in allusers)
                {
                    try
                    {
                        logger.LogInformation(string.Format("Starting update for {0}", item.UserPrincipalName));
                        var token = await tokenservice.RenewToken(item.RefreshToken);
                        var temp = new GraphServiceClient(new BearerTokenCredential(token));
                        var currentsubscription = await temp.Subscriptions[item.SubscriptionId].Request().GetAsync();
                        DateTimeOffset offset = DateTimeOffset.Now;
                        if (currentsubscription != null && currentsubscription.ExpirationDateTime!=null &&
                            currentsubscription.ExpirationDateTime.Value.CompareTo(offset.AddDays(1)) < 0)
                        {
                            currentsubscription.ExpirationDateTime = currentsubscription.ExpirationDateTime.Value.AddDays(1);
                            await temp.Subscriptions[item.SubscriptionId].Request().UpdateAsync(currentsubscription);
                            logger.LogInformation(string.Format("Completed update for {0}", item.UserPrincipalName));
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(string.Format("Exception for user {0}. Details of Error {1}",item.UserPrincipalName,ex));
                        continue;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError("Error while executing CRON job" + ex);
                throw;
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
}
