using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace WebPWrapper {
    /// <summary>
    /// WebP執行檔下載器
    /// </summary>
    public static class WebPExecuteDownloader {
        public const string _windowsUrl = "https://storage.googleapis.com/downloads.webmproject.org/releases/webp/libwebp-1.0.2-windows-x86.zip";
        public const string _linuxUrl = "https://storage.googleapis.com/downloads.webmproject.org/releases/webp/libwebp-1.0.2-linux-x86-64.tar.gz";
        public const string _osxUrl = "https://storage.googleapis.com/downloads.webmproject.org/releases/webp/libwebp-1.0.2-mac-10.14.tar.gz";

        public static async Task DownloadAsync() {
            string downloadUrl = null;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                downloadUrl = _windowsUrl;
            } else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
                downloadUrl = _linuxUrl;
            } else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
                downloadUrl = _osxUrl;
            } else {
                throw new PlatformNotSupportedException();
            }

            await Task.Run(async () => {
                var http = new HttpClient();

                var fileStream = await http.GetStreamAsync(downloadUrl);

                using (var zipFile = new ZipArchive(fileStream, ZipArchiveMode.Read)) {
                    if (Directory.Exists("webp")) {
                        Directory.Delete("webp", true);
                    }

                    zipFile.ExtractToDirectory("webp");
                }
            });
        }

        public static void Download() {
            DownloadAsync().GetAwaiter().GetResult();
        }
    }
}
