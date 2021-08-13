// ======================================
// BlazorSpread.net
// ======================================
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;
using System.Text.Json;

namespace BlobFaaS
{
    public static partial class Extensions
    {
        /// <summary>
        /// Returns a T value as JSON in HttpResponseData 
        /// </summary>
        public static HttpResponseData JsonResponse<T>(this HttpRequestData req, T value)
        {
            var res = req.CreateResponse(HttpStatusCode.OK);
            res.Headers.Add("Content-Type", "application/json; charset=utf-8");
            res.WriteString(JsonSerializer.Serialize<T>(value));
            return res;
        }
    }
}
