using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using HttpMultipartParser;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace BlobFaaS
{
    public class CreateBlob
    {
        readonly BlobServiceClient _blobServiceClient;

        public CreateBlob(BlobServiceClient blobServiceClient) => _blobServiceClient = blobServiceClient;

        [Function("CreateBlob")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "Post", Route = "CreateBlob/{container}")]
            HttpRequestData req,
            string container)
        {
            var result = false;
            try {
                var parsedFormBody = MultipartFormDataParser.ParseAsync(req.Body);
                var file = parsedFormBody.Result.Files[0];
                var blobContainer = _blobServiceClient.GetBlobContainerClient(container);
                var trustedName = WebUtility.HtmlEncode(file.FileName);
                var blobClient = blobContainer.GetBlobClient(trustedName);
                // if exists overwrite
                await blobClient.UploadAsync(file.Data);
                // set the content-type
                var headers = new BlobHttpHeaders {
                    ContentType = file.ContentType
                };
                blobClient.SetHttpHeaders(headers);
                result = true;
            }
            catch {/* report */}
            return req.JsonResponse(result);
        }
    }

    public class AppendBlob
    {
        readonly BlobServiceClient _blobServiceClient;

        public AppendBlob(BlobServiceClient blobServiceClient) => _blobServiceClient = blobServiceClient;

        [Function("AppendBlob")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "Post", Route = "AppendBlob/{container}/{fragment}")]
            HttpRequestData req,
            string container,
            long fragment,
            FunctionContext executionContext)
        {
            var _logger = executionContext.GetLogger("AppendBlob");
            var result = false;
            try {
                var parsedFormBody = MultipartFormDataParser.ParseAsync(req.Body);
                var file = parsedFormBody.Result.Files[0];
                var blobContainer = _blobServiceClient.GetBlobContainerClient(container);
                var trustedName = WebUtility.HtmlEncode(file.FileName);
                var blob = blobContainer.GetAppendBlobClient(trustedName);
                if (fragment == 0) {// create 
                    var headers = new BlobHttpHeaders {
                        ContentType = file.ContentType
                    };
                    await blob.CreateAsync(headers);
                    _logger.LogInformation($"Created: {container}/{trustedName}");
                }
                _logger.LogInformation($"Append fragment {fragment} to {trustedName}");
                await blob.AppendBlockAsync(file.Data);
                result = true;
            }
            catch (Exception e) {
                _logger.LogError($"Exception: {e.Message}");
            }
            return req.JsonResponse(result);
        }
    }

    public class GetBlobs
    {
        readonly BlobServiceClient _blobServiceClient;

        public GetBlobs(BlobServiceClient blobServiceClient) => _blobServiceClient = blobServiceClient;

        [Function("GetBlobs")]
        public async Task<IEnumerable<FileRecord>> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "GetBlobs/{container}")] HttpRequestData req,
            string container)
        {
            var ls = new List<FileRecord>();
            try {
                var blobContainer = _blobServiceClient.GetBlobContainerClient(container);
                await foreach (var blob in blobContainer.GetBlobsAsync()) {
                    ls.Add(new FileRecord(blob.Name, blob.Properties.ContentType, blob.Properties.ContentLength.Value));
                }
            }
            catch {/* report */}
            return ls;
        }
    }

    public class GetBlobContent
    {
        readonly BlobServiceClient _blobServiceClient;

        public GetBlobContent(BlobServiceClient blobServiceClient) => _blobServiceClient = blobServiceClient;

        [Function("GetBlobContent")]
        public async Task<byte[]> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "GetBlobContent/{container}/{name}")] HttpRequestData req,
            string container, string name)
        {
            try {
                var blobContainer = _blobServiceClient.GetBlobContainerClient(container);
                var blobClient = blobContainer.GetBlobClient(name);
                var contentType = blobClient.GetProperties().Value.ContentType;
                var file = await blobClient.DownloadAsync();
                // in a given case it need the type. It would need to return a tuple or custom type
                // var contentType = blobClient.GetProperties().Value.ContentType;
                await using var ms = new MemoryStream();
                await file.Value.Content.CopyToAsync(ms);
                return ms.ToArray();
            }
            catch  {/* report */}
            return default;
        }
    }

    public class DeleteBlob
    {
        readonly BlobServiceClient _blobServiceClient;

        public DeleteBlob(BlobServiceClient blobServiceClient) => _blobServiceClient = blobServiceClient;

        [Function("DeleteBlob")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "DeleteBlob/{container}/{name}")] HttpRequestData req,
            string container, string name)
        {
            var blobContainer = _blobServiceClient.GetBlobContainerClient(container);
            var blobClient = blobContainer.GetBlobClient(name);
            var result = false;
            try {
                await blobClient.DeleteIfExistsAsync();
                result= true;
            }
            catch {/* report */}
            return req.JsonResponse(result);
        }
    }

    #region MODEL
    public record FileRecord(string FileName, string ContentType, long Size);
    public record FileData(string FileName, string ContentType, Stream Data);
    #endregion
}
