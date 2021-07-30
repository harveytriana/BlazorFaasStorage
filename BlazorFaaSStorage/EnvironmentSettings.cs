using System;

namespace BlazorFaaSStorage
{
    /*
    Environment Settings
    1. Hide ConnectionString in shared code
    2. Allow interchange into LocalaStorage and Azure without compile
    3. In Azure will use AzureWebJobsStorage created self in publish Function App
    SET envirotment variables, from command line:
    Windows (Adminsitrator): > setx AzureFunctionsApiRoot "VALUE" /m 
    Linux: > export AzureFunctionsApiRoot="VALUE"
    */
    public static class EnvironmentSettings
    {
        public static string GetAzureFunctionsApiRoot()
        {
            var apiRoot = Environment.GetEnvironmentVariable("AzureFunctionsApiRoot");

            Console.WriteLine("apiRoot: {0}", apiRoot??"null");

            return apiRoot ?? "http://localhost:7071";
        }
    }
}

