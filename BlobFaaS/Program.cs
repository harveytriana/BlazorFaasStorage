using Azure.Storage.Blobs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;

namespace BlobFaaS
{
    public class Program
    {
        public static void Main()
        {
            var storageConnectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");

            var host = new HostBuilder()
                .ConfigureFunctionsWorkerDefaults()
                .ConfigureServices(services => {
                    // Add Logging
                    services.AddLogging();
                    // blob services
                    services.AddScoped(_ => new BlobServiceClient(storageConnectionString));
                })
                .Build();
            host.Run();
        }
    }
}