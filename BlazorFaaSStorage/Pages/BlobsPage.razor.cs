// ======================================
// BlazorSpread.net
// ======================================
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using BlazorFaaSStorage.Components;

namespace BlazorFaaSStorage.Pages
{
    public partial class BlobsPage : ComponentBase
    {
        [Inject] IHttpClientFactory ClientFactory { get; set; }
        [Inject] IJSRuntime Js { get; set; }

        HttpClient _httpClient;
        List<string> containers;
        List<FileRecord> blobs;
        FileRecord fileRecord;
        string container;
        string imageDataUrl;
        string imageFile;
        string echo;

        protected override async Task OnInitializedAsync()
        {
            _httpClient = ClientFactory.CreateClient(Program.Serverless);
            await PopulateContainersList();
        }

        async ValueTask PopulateContainersList()
        {
            echo = "Loading...";
            try {
                var q = await GetContainers();
                containers = q.ToList();
                // set popups (runs after render)
                await Task.Delay(200); // important

                await Js.InvokeVoidAsync("contextMenuSetup", "container-actions");
                // load first container
                if (containers.Any()) {
                    container = containers.First();
                    await PopulateBlobsList();
                }
                echo = $"Containers: {containers.Count}";
            }
            catch (Exception exception) {
                echo = "Exception: " + exception.Message;
            }
        }

        async Task ContainerChange(ChangeEventArgs e)
        {
            // load containes blobs
            container = e.Value.ToString();
            await PopulateBlobsList();
        }

        async Task GetImage()
        {
            if (fileRecord.ContentType.IsContentImage()) {
                echo = "Loading...";
                var data = await GetFileContent(container, fileRecord.FileName);
                if (data != null) {
                    imageDataUrl = $"data:{fileRecord.ContentType};base64,{Convert.ToBase64String(data)}";
                    imageFile = fileRecord.FileName;
                    echo = imageFile;
                }
                else { echo = $"Not found."; }
            }
            else { echo = "File is not image."; }
        }

        async Task DownloadFile()
        {
            echo = $"Download: {container}/{fileRecord.FileName}";
            await InvokeAsync(StateHasChanged);
            // Delegate the task to the browser to download the file, just pass the URI
            var url = $"{Program.ApiRoot}/api/GetBlobContent/{container}/{fileRecord.FileName}";
            await Js.InvokeVoidAsync("performDownloadFetch", url, fileRecord.FileName, fileRecord.ContentType);
        }

        async Task DeleteBlob()
        {
            if (await DeleteFile(container, fileRecord.FileName)) {
                blobs.Remove(blobs.First(x => x.FileName == fileRecord.FileName));
                if (imageFile == fileRecord.FileName) {
                    imageDataUrl = null;
                }
                echo = $"Deleted: {fileRecord.FileName}";
            }
            else { echo = "An exception occurred while trying to delete the document."; }
        }

        async ValueTask PopulateBlobsList()
        {
            var q = await GetFiles(container);
            blobs = q.ToList();
            // set popups (needs after render)
            await InvokeAsync(StateHasChanged);
            await Js.InvokeVoidAsync("contextMenuSetup", "blob-actions");
            // clear image
            imageDataUrl = null;
            echo = $"{container}, {blobs.Count} blobs";
        }

        /// <summary>
        /// Execute the acion from command in popup menu
        /// </summary>
        async Task BlobAction(int index)
        {
            if (uploading) {
                echo = "Is uploading...";
                return;
            }
            // target object is fileRecord, that was let in LetFileRecord()
            switch (index) {
                case 1:
                await GetImage();
                break;
                case 2:
                await DownloadFile();
                break;
                case 3:
                await DeleteBlob();
                break;
                case 4:
                await RefreshContainer();
                break;
                case 5:
                CreateContainerDialog();
                break;
                case 6:
                DeleteContainerDialog();
                break;
            }
        }

        void LetFileRecord(FileRecord item)
        {
            fileRecord = item;
        }

        async Task RefreshContainer()
        {
            echo = "Refreshing...";
            await PopulateBlobsList();
        }

        #region Uploader
        const int MAXFILES = 32;
        const long HOTSIZE = 104857600 / 2; // 50 MG (max allow request)
        bool uploading;

        readonly Dictionary<string, object> browseAttributes = new()
        {
            { "style", "display:none" }, // for custon label
            { "id", "browse-files" }     // for custom label
        };

        async Task UploadFiles(InputFileChangeEventArgs e)
        {
            int uploads = 0;
            uploading = true;
            bool result;
            foreach (var file in e.GetMultipleFiles(MAXFILES)) {
                echo = $"Uploading {file.Name}, {file.Size.ToFileSize()} ...";
                if (file.Size <= HOTSIZE) {
                    result = await UploadFile(file);
                }
                else {
                    result = await UploadBigFile(file);
                }
                if (result) {
                    uploads++;
                    blobs.Add(new FileRecord(file.Name, file.ContentType.FixContentType(), file.Size));
                }
            }
            echo = $"Load {uploads} files";
            if (uploads > 0) {// subscribe menu event
                await InvokeAsync(StateHasChanged);
                await Js.InvokeVoidAsync("contextMenuSetup", "blob-actions");
                //TODO Scroll
            }
            uploading = false;
        }

        async Task<bool> UploadFile(IBrowserFile file)
        {
            var fileData = new FileData(file.Name, file.ContentType.FixContentType(), file.OpenReadStream(long.MaxValue));
            return await UploadFile(container, fileData);
        }

        async Task<bool> UploadBigFile(IBrowserFile file)
        {
            const long CHUNKSIZE = 1000_000; // subjective
            long uploadedBytes = 0;
            long totalBytes = file.Size;
            long percent = 0;
            long fragment = 0;
            long chunkSize;
            long fragments = totalBytes / CHUNKSIZE + 1;
            var now = DateTime.Now;

            using (var stream = file.OpenReadStream(long.MaxValue)) {
                uploading = true;
                while (uploading) {
                    chunkSize = CHUNKSIZE;
                    if (uploadedBytes + CHUNKSIZE > totalBytes) {// remainder
                        chunkSize = totalBytes - uploadedBytes;
                    }
                    var chunk = new byte[chunkSize];
                    await stream.ReadAsync(chunk, 0, chunk.Length);

                    var fileData = new FileData(file.Name, file.ContentType.FixContentType(), new MemoryStream(chunk));
                    if (await AppendFile(container, fragment++, fileData) == false) {
                        echo = "Can not append blob.";
                        return false;
                    }
                    // Update our progress data and UI
                    uploadedBytes += chunkSize;
                    percent = uploadedBytes * 100 / totalBytes;
                    echo = $"Uploaded {percent}%  Fragment {fragment} of {fragments} | {(DateTime.Now - now).TotalSeconds:N6}";
                    if (percent >= 100) {// upload complete
                        uploading = false;
                    }
                    await InvokeAsync(StateHasChanged);
                }
            }
            return true;
        }
        #endregion

        #region Dialogs
        BasicDialog dialog;

        void CreateContainerDialog()
        {
            dialog.InputDialog("New container name (use lower case, no spaces):", "Containers");
        }

        async Task CreateContainerDialogResult(string value)
        {
            if (value.Empty()) {
                return;
            }
            var name = value.ToLower();
            if (containers.Contains(name)) {
                echo = $"'{name}' anready exist.";
                return;
            }
            if (await CreateContainer(name)) {
                containers.Add(name);
                container = name;
                blobs.Clear();
                imageDataUrl = null;
                await InvokeAsync(StateHasChanged);
            }
        }

        void DeleteContainerDialog()
        {
            var q = $"By accepting, you will delete the container '{container}'"
                  + " with all your files. Are you sure to continue?";
            dialog.QuestionDialog(q, "Containers");
        }

        async Task DeleteContainerDialogResult(bool value)
        {
            if (value == false) {
                return;
            }
            if (containers.Count == 1) {
                echo = "Cannot delete the unique container in the store.";
                return;
            }
            echo = $"Deleting {container}...";
            if (await DeleteContainer(container)) {
                containers.Remove(container);
                container = containers.First();
                await PopulateBlobsList();
            }
        }
        #endregion

        #region CONTAINERS
        public async ValueTask<IEnumerable<string>> GetContainers()
        {
            var url = $"api/GetContainers";
            return await _httpClient.GetFromJsonAsync<IEnumerable<string>>(url);
        }

        public async ValueTask<bool> CreateContainer(string name)
        {
            var url = $"api/CreateContainer/{name}";
            return await _httpClient.GetFromJsonAsync<bool>(url);
        }

        public async ValueTask<bool> DeleteContainer(string name)
        {
            var url = $"api/DeleteContainer/{name}";
            return await _httpClient.GetFromJsonAsync<bool>(url);
        }
        #endregion

        #region BLOBS 
        public async ValueTask<IEnumerable<FileRecord>> GetFiles(string container)
        {
            var url = $"api/GetBlobs/{container}";
            return await _httpClient.GetFromJsonAsync<IEnumerable<FileRecord>>(url);
        }

        public async ValueTask<bool> DeleteFile(string container, string name)
        {
            var url = $"api/DeleteBlob/{container}/{name}";
            return await _httpClient.GetFromJsonAsync<bool>(url);
        }

        public async Task<byte[]> GetFileContent(string container, string name)
        {
            var url = $"api/GetBlobContent/{container}/{name}";
            var response = await _httpClient.GetAsync(url);
            return await response.Content.ReadAsByteArrayAsync();
        }

        public async ValueTask<bool> UploadFile(string container, FileData file)
        {
            var url = $"api/CreateBlob/{container}";
            // create HttpContent
            using var formFile = new MultipartFormDataContent();
            var fileContent = new StreamContent(file.Data);
            fileContent.Headers.Add("Content-Type", file.ContentType);
            formFile.Add(fileContent, "file", file.FileName);

            var response = await _httpClient.PostAsync(url, formFile);
            return await response.Content.ReadFromJsonAsync<bool>();
        }

        public async ValueTask<bool> AppendFile(string container, long fragment, FileData file)
        {
            var url = $"api/AppendBlob/{container}/{fragment}";
            // create HttpContent
            using var formFile = new MultipartFormDataContent();
            var fileContent = new StreamContent(file.Data); // it is a fragment
            formFile.Add(fileContent, "file", file.FileName);
            //
            var response = await _httpClient.PostAsync(url, formFile);
            return await response.Content.ReadFromJsonAsync<bool>();
        }
        #endregion
    }

    #region MODEL
    public record FileRecord(string FileName, string ContentType, long Size);
    public record FileData(string FileName, string ContentType, Stream Data);
    #endregion
}
