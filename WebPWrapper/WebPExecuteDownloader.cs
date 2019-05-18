using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Tar;
using ICSharpCode.SharpZipLib.Zip;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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

        public static async Task DownloadAsync(bool ignoreIfExtsis = true) {
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

                var path = Path.Combine(Path.GetFullPath("."), "webp");

                if (Directory.Exists(path)) {
                    if (ignoreIfExtsis) {
                        return;
                    }
                    Directory.Delete(path, true);
                }

                Directory.CreateDirectory(path);

                using (var zipFileStream = File.Open(Path.Combine(path, "webp.bin"), FileMode.Create)) {
                    await fileStream.CopyToAsync(zipFileStream);
                }

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                    var fast = new FastZip();
                    fast.ExtractZip(Path.Combine(path, "webp.bin"), path, null);
                } else {
                    Stream inStream = File.OpenRead(Path.Combine(path, "webp.bin"));
                    Stream gzipStream = new GZipInputStream(inStream);

                    TarArchive tarArchive = TarArchive.CreateInputTarArchive(gzipStream);
                    tarArchive.ExtractContents(path);
                    tarArchive.Close();

                    gzipStream.Close();
                    inStream.Close();

                    var executeFilesList = Exec($"find {path} | grep bin/")
                        .Split(new char[] { '\r', '\n' },
                        StringSplitOptions.RemoveEmptyEntries);

                    foreach (var executeFile in executeFilesList) {
                        Exec($"chmod +x {executeFile}");
                    }
                }
            });
        }

        // Copy From : https://stackoverflow.com/questions/45132081/file-permissions-on-linux-unix-with-net-core
        private static string Exec(string cmd) {
            var escapedArgs = cmd.Replace("\"", "\\\"");

            var process = new Process {
                StartInfo = new ProcessStartInfo {
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    FileName = "bash",
                    Arguments = $"-c \"{escapedArgs}\""
                }
            };
            string stdout = "";
            process.OutputDataReceived += (object sender, DataReceivedEventArgs e) => {
                stdout += e.Data + "\r\n";
            };
            process.ErrorDataReceived += (object sender, DataReceivedEventArgs e) => {
                stdout += e.Data + "\r\n";
            };
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.RedirectStandardOutput = true;

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();

            return stdout;
        }

        public static void Download(bool ignoreIfExtsis = true) {
            DownloadAsync(ignoreIfExtsis).GetAwaiter().GetResult();
        }
    }
}
