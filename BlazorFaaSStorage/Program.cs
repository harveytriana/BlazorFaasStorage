using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace BlazorFaaSStorage
{
    public class Program
    {
        public static string Serverless { get; private set; } = "Serverless";
        public static string ApiRoot { get; private set; }

        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.RootComponents.Add<App>("#app");

            ApiRoot = builder.Configuration.GetSection("ApiRoot").Get<string>();

            // default 
            builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

            // IHttpClientFactory: needs PackageReference Microsoft.Extensions.Http
            builder.Services.AddHttpClient(Serverless, _ => _.BaseAddress = new Uri(ApiRoot));

            await builder.Build().RunAsync();
        }
    }
}
