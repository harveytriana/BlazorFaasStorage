using System.Collections.Generic;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace BlobFaaS
{
    public class GetContainers
    {
        readonly BlobServiceClient _blobServiceClient;

        public GetContainers(BlobServiceClient blobServiceClient) => _blobServiceClient = blobServiceClient;

        [Function("GetContainers")]
        public async ValueTask<IEnumerable<string>> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req)
        {
            var ls = new List<string>();
            try {
                await foreach (var container in _blobServiceClient.GetBlobContainersAsync()) {
                    var name = container.Name;
                    if (name.StartsWith("azure") ||
                        name.StartsWith("$")) {// system container
                        continue;
                    }
                    ls.Add(name);
                }
            }
            catch {/* report */}
            return ls;
        }
    }

    public class CreateContainer
    {
        readonly BlobServiceClient _blobServiceClient;

        public CreateContainer(BlobServiceClient blobServiceClient) => _blobServiceClient = blobServiceClient;

        [Function("CreateContainer")]
        public async ValueTask<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "CreateContainer/{name}")] HttpRequestData req,
            string name)
        {
            var result = false;
            try {
                await _blobServiceClient.CreateBlobContainerAsync(name, PublicAccessType.BlobContainer);
                result = true;
            }
            catch {/* report */}
            return req.JsonResponse(result);
        }
    }

    public class DeleteContainer
    {
        readonly BlobServiceClient _blobServiceClient;

        public DeleteContainer(BlobServiceClient blobServiceClient) => _blobServiceClient = blobServiceClient;

        [Function("DeleteContainer")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "DeleteContainer/{name}")] HttpRequestData req,
            string name)
        {
            var result = false;
            try {
                await _blobServiceClient.DeleteBlobContainerAsync(name);
                result = true;
            }
            catch {/* report */}
            return req.JsonResponse(result);
        }
    }

}
