using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace WebPWrapper.Test {
    public class WebPExecuteDownloadHelper : IDisposable {
        public WebPExecuteDownloadHelper() {
            WebPExecuteDownloader.Download();
        }

        public void Dispose() {
        }

    }

    [CollectionDefinition("WebP Init")]
    public class DatabaseCollection : ICollectionFixture<WebPExecuteDownloadHelper> {
    }
}
