using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.IO.Compression;

namespace WebPWrapper {
    public static class WebPExecuteDownloader {
        public const string _windowsUrl = "https://storage.googleapis.com/downloads.webmproject.org/releases/webp/libwebp-1.2.2-windows-x64.zip";
        public const string _linuxUrl = "https://storage.googleapis.com/downloads.webmproject.org/releases/webp/libwebp-1.2.2-linux-x86-64.tar.gz";
        public const string _osxUrl = "https://storage.googleapis.com/downloads.webmproject.org/releases/webp/libwebp-1.2.2-mac-x86-64.tar.gz";

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
                    ZipFile.ExtractToDirectory(Path.Combine(path, "webp.bin"), path, null);
                } else {
                    using (var stream = File.OpenRead(Path.Combine(path, "webp.bin")))
                        ExtractTarGz(stream, Path.Combine(path, "webp.bin"));

                    var executeFilesList = Exec($"find {path} | grep bin/")
                        .Split(new char[] { '\r', '\n' },
                        StringSplitOptions.RemoveEmptyEntries);

                    foreach (var executeFile in executeFilesList) {
                        Exec($"chmod +x {executeFile}");
                    }
                }
            });
        }

        public static void ExtractTarGz(string filename, string outputDir)
        {
            using (var stream = File.OpenRead(filename))
                ExtractTarGz(stream, outputDir);
        }

        /// <summary>
        /// Extracts a <i>.tar.gz</i> archive stream to the specified directory.
        /// </summary>
        /// <param name="stream">The <i>.tar.gz</i> to decompress and extract.</param>
        /// <param name="outputDir">Output directory to write the files.</param>
        public static void ExtractTarGz(Stream stream, string outputDir)
        {
            // A GZipStream is not seekable, so copy it first to a MemoryStream
            using (var gzip = new GZipStream(stream, CompressionMode.Decompress))
            {
                const int chunk = 4096;
                using (var memStr = new MemoryStream())
                {
                    int read;
                    var buffer = new byte[chunk];
                    do
                    {
                        read = gzip.Read(buffer, 0, chunk);
                        memStr.Write(buffer, 0, read);
                    } while (read == chunk);

                    memStr.Seek(0, SeekOrigin.Begin);
                    ExtractTar(memStr, outputDir);
                }
            }
        }

        /// <summary>
        /// Extractes a <c>tar</c> archive to the specified directory.
        /// </summary>
        /// <param name="filename">The <i>.tar</i> to extract.</param>
        /// <param name="outputDir">Output directory to write the files.</param>
        public static void ExtractTar(string filename, string outputDir)
        {
            using (var stream = File.OpenRead(filename))
                ExtractTar(stream, outputDir);
        }

        /// <summary>
        /// Extractes a <c>tar</c> archive to the specified directory.
        /// </summary>
        /// <param name="stream">The <i>.tar</i> to extract.</param>
        /// <param name="outputDir">Output directory to write the files.</param>
        public static void ExtractTar(Stream stream, string outputDir)
        {
            var buffer = new byte[100];
            while (true)
            {
                stream.Read(buffer, 0, 100);
                var name = Encoding.ASCII.GetString(buffer).Trim('\0');
                if (String.IsNullOrWhiteSpace(name))
                    break;
                stream.Seek(24, SeekOrigin.Current);
                stream.Read(buffer, 0, 12);
                var size = Convert.ToInt64(Encoding.UTF8.GetString(buffer, 0, 12).Trim('\0').Trim(), 8);

                stream.Seek(376L, SeekOrigin.Current);

                var output = Path.Combine(outputDir, name);
                if (!Directory.Exists(Path.GetDirectoryName(output)))
                    Directory.CreateDirectory(Path.GetDirectoryName(output));
                if (!name.Equals("./", StringComparison.InvariantCulture))
                {
                    using (var str = File.Open(output, FileMode.OpenOrCreate, FileAccess.Write))
                    {
                        var buf = new byte[size];
                        stream.Read(buf, 0, buf.Length);
                        str.Write(buf, 0, buf.Length);
                    }
                }

                var pos = stream.Position;

                var offset = 512 - (pos % 512);
                if (offset == 512)
                    offset = 0;

                stream.Seek(offset, SeekOrigin.Current);
            }
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
