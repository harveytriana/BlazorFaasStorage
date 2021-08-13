using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Azure.Storage.Blobs;

namespace BlobFaaS
{
    public class Program
    {
        public static void Main()
        {
            // contains storage connection string
            var storageConnectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");

            var host = new HostBuilder()
                .ConfigureFunctionsWorkerDefaults()
                .ConfigureServices(services => {
                    // Logging 
                    services.AddLogging();
                    // blob services
                    services.AddScoped(_ => new BlobServiceClient(storageConnectionString));
                })
                .Build();
            host.Run();
        }
    }
}