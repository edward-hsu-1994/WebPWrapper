using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.IO.Compression;

namespace WebPWrapper
{
    /// <summary>
    /// WebP Cli downloader
    /// </summary>
    public static class WebPExecuteDownloader
    {
        public const string WELL_KNOWN_WINDOWS_CLI_URL =
            "https://storage.googleapis.com/downloads.webmproject.org/releases/webp/libwebp-1.3.1-windows-x64.zip";

        public const string WELL_KNOWN_LINUX_CLI_URL =
            "https://storage.googleapis.com/downloads.webmproject.org/releases/webp/libwebp-1.3.1-linux-x86-64.tar.gz";

        public const string WELL_KNOWN_OSX_CLI_URL =
            "https://storage.googleapis.com/downloads.webmproject.org/releases/webp/libwebp-1.3.1-mac-x86-64.tar.gz";

        public const string WELL_KNOWN_OSX_ARM_CLI_URL =
            "https://storage.googleapis.com/downloads.webmproject.org/releases/webp/libwebp-1.3.1-mac-arm64.tar.gz";

        /// <summary>
        /// Download CLI binary file.
        /// </summary>
        /// <param name="ignoreIfExtsis">When the CLI already exists, it will not be downloaded</param>
        /// <returns></returns>
        /// <exception cref="PlatformNotSupportedException"></exception>
        public static async Task DownloadAsync(bool ignoreIfExtsis = true)
        {
            string downloadUrl = null;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                downloadUrl = WELL_KNOWN_WINDOWS_CLI_URL;
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                downloadUrl = WELL_KNOWN_LINUX_CLI_URL;
            }
            else if(RuntimeInformation.ProcessArchitecture == Architecture.Arm64 && RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                downloadUrl = WELL_KNOWN_OSX_ARM_CLI_URL;
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                downloadUrl = WELL_KNOWN_OSX_CLI_URL;
            }
            else
            {
                throw new PlatformNotSupportedException();
            }

            await Task.Run(async () =>
            {
                using var http = new HttpClient();

                var webpCliDownloadStream = await http.GetStreamAsync(downloadUrl);

                var webpCliRootDirectoryPath = Path.Combine(Path.GetFullPath("."), "webp");

                if (Directory.Exists(webpCliRootDirectoryPath))
                {
                    if (ignoreIfExtsis)
                    {
                        return;
                    }

                    Directory.Delete(webpCliRootDirectoryPath, true);
                }

                // Create WebP CLI root directory
                Directory.CreateDirectory(webpCliRootDirectoryPath);

                // Saving WebP Cli
                var origionalDownloadWebCliFilePath = Path.GetTempFileName();
                using (var origionalDownloadWebCliFileStream =
                       File.Open(origionalDownloadWebCliFilePath, FileMode.OpenOrCreate))
                {
                    await webpCliDownloadStream.CopyToAsync(origionalDownloadWebCliFileStream);
                }

                var downloadUrlExtension = Path.GetExtension(downloadUrl)?.ToLower();
                if (downloadUrlExtension == ".gz")
                {
                    var webpCliTarFilePath = Path.GetTempFileName();
                    using (var webpCliTarGzFileStream = File.Open(origionalDownloadWebCliFilePath, FileMode.Open))
                    using (var webpCliTarFileStream = File.Open(webpCliTarFilePath, FileMode.OpenOrCreate))
                    using (var downloadedWebPCliTarStream =
                           new GZipStream(webpCliTarGzFileStream, CompressionMode.Decompress))
                    {
                        await downloadedWebPCliTarStream.CopyToAsync(webpCliTarFileStream);
                    }

                    File.Delete(origionalDownloadWebCliFilePath);
                    File.Move(webpCliTarFilePath, origionalDownloadWebCliFilePath);

                    TarFile.ExtractToDirectory(origionalDownloadWebCliFilePath, webpCliRootDirectoryPath, null);

                    var executeFilesList = Exec($"find {webpCliRootDirectoryPath} | grep bin/")
                        .Split(new char[] { '\r', '\n' },
                            StringSplitOptions.RemoveEmptyEntries);

                    foreach (var executeFile in executeFilesList)
                    {
                        Exec($"chmod +x {executeFile}");
                    }
                }
                else
                {
                    ZipFile.ExtractToDirectory(origionalDownloadWebCliFilePath, webpCliRootDirectoryPath, null);
                }

                File.Delete(origionalDownloadWebCliFilePath);
            });
        }

        /// <summary>
        /// Download CLI binary file
        /// </summary>
        /// <param name="ignoreIfExtsis">When the CLI already exists, it will not be downloaded</param>
        public static void Download(bool ignoreIfExtsis = true)
        {
            DownloadAsync(ignoreIfExtsis).GetAwaiter().GetResult();
        }

        // Copy From : https://stackoverflow.com/questions/45132081/file-permissions-on-linux-unix-with-net-core
        private static string Exec(string cmd)
        {
            var escapedArgs = cmd.Replace("\"", "\\\"");

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    FileName = "bash",
                    Arguments = $"-c \"{escapedArgs}\""
                }
            };
            string stdout = "";
            process.OutputDataReceived += (object sender, DataReceivedEventArgs e) => { stdout += e.Data + "\r\n"; };
            process.ErrorDataReceived += (object sender, DataReceivedEventArgs e) => { stdout += e.Data + "\r\n"; };
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.RedirectStandardOutput = true;

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();

            return stdout;
        }
    }
}
