/* By: Harvey Triana */

// url returns FileResultContent
function performDownload(uri) {
    window.open(uri, '_self');
}

// Uri returns byte[]
function performDownloadFetch(uri, fileName, contentType) {
    fetch(uri).then(function (response) {
        return response.arrayBuffer();
    }).then(function (buffer) {
        let blob = new Blob([buffer], {
            type: contentType
        });
        let link = document.createElement('a');
        link.href = window.URL.createObjectURL(blob);
        link.download = fileName;
        link.click();
    });
}

