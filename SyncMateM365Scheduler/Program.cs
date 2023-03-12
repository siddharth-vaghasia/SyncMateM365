using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;

namespace SyncMateM365Scheduler
{
    class Program
    {
        public static async Task Main()
        {
            var services = new ServiceCollection();


            var builder = new HostBuilder();
            builder.ConfigureWebJobs(b =>
            {
                b.AddAzureStorageCoreServices();
                b.AddTimers();
            });
            builder.ConfigureLogging((context, b) =>
            {
                b.AddConsole();
            });
            var host = builder.Build();
            using (host)
            {
                var jobHost = host.Services.GetService(typeof(IJobHost)) as JobHost;
                await host.StartAsync();
                if (jobHost != null)
                {
                    await jobHost.CallAsync("SyncMateM365Scheduler");
                }
                await host.StopAsync();
            }
        }
    }
}