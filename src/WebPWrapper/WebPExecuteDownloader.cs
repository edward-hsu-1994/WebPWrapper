using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WebPWrapper
{
    /// <summary>
    /// Downloads and installs the platform-specific WebP command-line tools.
    /// </summary>
    public static class WebPExecuteDownloader
    {
        /// <summary>Official Windows x64 WebP CLI archive URL.</summary>
        public const string WELL_KNOWN_WINDOWS_CLI_URL =
            "https://storage.googleapis.com/downloads.webmproject.org/releases/webp/libwebp-1.6.0-windows-x64.zip";

        /// <summary>Official Linux x64 WebP CLI archive URL.</summary>
        public const string WELL_KNOWN_LINUX_CLI_URL =
            "https://storage.googleapis.com/downloads.webmproject.org/releases/webp/libwebp-1.6.0-linux-x86-64.tar.gz";

        /// <summary>Official macOS x64 WebP CLI archive URL.</summary>
        public const string WELL_KNOWN_OSX_CLI_URL =
            "https://storage.googleapis.com/downloads.webmproject.org/releases/webp/libwebp-1.6.0-mac-x86-64.tar.gz";

        /// <summary>Official macOS ARM64 WebP CLI archive URL.</summary>
        public const string WELL_KNOWN_OSX_ARM_CLI_URL =
            "https://storage.googleapis.com/downloads.webmproject.org/releases/webp/libwebp-1.6.0-mac-arm64.tar.gz";

        private const string CliVersion = "1.6.0";
        private const long MaxDownloadBytes = 64L * 1024 * 1024;
        private const long MaxExpandedArchiveBytes = 320L * 1024 * 1024;
        private const long MaxExpandedBytes = 256L * 1024 * 1024;
        private const long MaxEntryBytes = 128L * 1024 * 1024;
        private static readonly TimeSpan InstallationLockTimeout = TimeSpan.FromSeconds(30);
        private static readonly TimeSpan InstallationLockRetryInterval = TimeSpan.FromMilliseconds(100);
        private static readonly HttpClient HttpClient = new HttpClient();

        private const string WindowsSha256 = "48886f506b21f62e4661f0f4cbfca19800897c385128e8902542d29a950c93f1";
        private const string LinuxSha256 = "1c5ffab71efecefa0e3c23516c3a3a1dccb45cc310ae1095c6f14ae268e38067";
        private const string MacX64Sha256 = "f112dd83b420ab2a4b27d46610d9827ddf4200216023281de378647ecca31c2a";
        private const string MacArm64Sha256 = "bc6bf84cc70f3f8574fba797d1e4a7dea4feebe9fa4be919f202413ea2b3b8f2";

        /// <summary>
        /// Downloads, verifies, and installs the platform WebP CLI archive.
        /// Existing installations are left untouched unless replacement succeeds.
        /// </summary>
        /// <param name="ignoreIfExtsis">When true, return without downloading if the current CLI executables exist.</param>
        /// <returns>A task that completes after installation or the no-op check.</returns>
        /// <exception cref="PlatformNotSupportedException">The current platform has no supported archive.</exception>
        /// <exception cref="InvalidDataException">The downloaded archive fails integrity or safety checks.</exception>
        public static Task DownloadAsync(bool ignoreIfExtsis = true)
        {
            return DownloadAsync(ignoreIfExtsis, CancellationToken.None);
        }
        /// <summary>Downloads and installs the platform CLI, allowing the operation to be canceled.</summary>
        /// <param name="cancellationToken">Token used to cancel the HTTP request, response-body reads, and lock wait.</param>
        /// <exception cref="OperationCanceledException">The operation was canceled.</exception>
        public static Task DownloadAsync(CancellationToken cancellationToken)
        {
            return DownloadAsync(true, cancellationToken);
        }

        /// <summary>Downloads and installs the platform CLI with an optional existing-installation check and cancellation.</summary>
        /// <param name="ignoreIfExtsis">When true, return without downloading if the current CLI executables exist.</param>
        /// <param name="cancellationToken">Token used to cancel the HTTP request, response-body reads, and lock wait.</param>
        /// <exception cref="OperationCanceledException">The operation was canceled.</exception>
        /// <exception cref="TimeoutException">The installation lock was unavailable for 30 seconds.</exception>
        public static async Task DownloadAsync(bool ignoreIfExtsis, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var (downloadUrl, expectedSha256, encoderRelativePath, decoderRelativePath) = GetPlatformArchive();
            var installPath = GetDefaultInstallationPath();
            if (ignoreIfExtsis && IsCurrentInstallationComplete(installPath, encoderRelativePath, decoderRelativePath))
                return;
            var installParent = Path.GetDirectoryName(installPath);
            Directory.CreateDirectory(installParent);

            using (await AcquireInstallationLockAsync(
                Path.Combine(installParent, ".webp.install.lock"), cancellationToken).ConfigureAwait(false))
            {
                if (ignoreIfExtsis && IsCurrentInstallationComplete(installPath, encoderRelativePath, decoderRelativePath))
                    return;

                using (var response = await HttpClient.GetAsync(
                    downloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false))
                {
                    response.EnsureSuccessStatusCode();
                    using (var responseStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                    {
                        await InstallArchiveAsync(
                            responseStream, downloadUrl, installPath, expectedSha256, cancellationToken).ConfigureAwait(false);
                    }
                }
            }
        }


        /// <summary>
        /// Returns the stable per-user installation directory for the current libwebp release.
        /// </summary>
        internal static string GetDefaultInstallationPath()
        {
            return GetVersionedInstallationPath(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                CliVersion);
        }

        internal static string GetVersionedInstallationPath(string localApplicationDataPath, string version)
        {
            return Path.Combine(localApplicationDataPath, "WebPWrapper", "libwebp", version);
        }

        internal static string GetDefaultExecutablePath(bool encoder)
        {
            var (_, _, encoderRelativePath, decoderRelativePath) = GetPlatformArchive();
            var relativePath = encoder ? encoderRelativePath : decoderRelativePath;
            var cachedPath = Path.Combine(GetDefaultInstallationPath(), relativePath);
            if (File.Exists(cachedPath))
                return cachedPath;

            var legacyPath = Path.Combine(Path.GetFullPath("webp"), relativePath);
            return File.Exists(legacyPath) ? legacyPath : cachedPath;
        }
        /// <summary>
        /// Downloads, verifies, and installs the platform WebP CLI archive synchronously.
        /// </summary>
        /// <param name="ignoreIfExtsis">When true, return without downloading if the CLI directory exists.</param>
        public static void Download(bool ignoreIfExtsis = true)
        {
            DownloadAsync(ignoreIfExtsis).GetAwaiter().GetResult();
        }
        /// <summary>Downloads and installs the platform CLI synchronously with cancellation.</summary>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        public static void Download(CancellationToken cancellationToken)
        {
            DownloadAsync(cancellationToken).GetAwaiter().GetResult();
        }

        /// <summary>Downloads and installs the platform CLI synchronously with an existing-installation check and cancellation.</summary>
        /// <param name="ignoreIfExtsis">When true, return without downloading if the current CLI executables exist.</param>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        public static void Download(bool ignoreIfExtsis, CancellationToken cancellationToken)
        {
            DownloadAsync(ignoreIfExtsis, cancellationToken).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Stages an archive, checks its pinned digest, safely extracts it, and replaces the installation.
        /// </summary>
        /// <param name="archiveStream">Readable stream containing the downloaded archive.</param>
        /// <param name="archiveName">Fixed release archive URL or name, used to select ZIP versus TAR.GZ.</param>
        /// <param name="installPath">Destination installation directory.</param>
        /// <param name="expectedSha256">Trusted lowercase or uppercase SHA-256 digest.</param>
        /// <exception cref="InvalidDataException">The archive exceeds limits or has a digest mismatch.</exception>
        internal static Task InstallArchiveAsync(
            Stream archiveStream,
            string archiveName,
            string installPath,
            string expectedSha256)
        {
            return InstallArchiveAsync(
                archiveStream, archiveName, installPath, expectedSha256, CancellationToken.None);
        }

        internal static async Task InstallArchiveAsync(
            Stream archiveStream,
            string archiveName,
            string installPath,
            string expectedSha256,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (archiveStream == null)
                throw new ArgumentNullException(nameof(archiveStream));
            if (string.IsNullOrWhiteSpace(archiveName))
                throw new ArgumentException("An archive name is required.", nameof(archiveName));
            if (string.IsNullOrWhiteSpace(installPath))
                throw new ArgumentException("An installation path is required.", nameof(installPath));
            if (expectedSha256 == null || expectedSha256.Length != 64)
                throw new ArgumentException("A 64-character SHA-256 digest is required.", nameof(expectedSha256));

            installPath = Path.GetFullPath(installPath);
            var parentDirectory = Path.GetDirectoryName(installPath);
            Directory.CreateDirectory(parentDirectory);
            if (File.Exists(installPath))
                throw new IOException("The WebP installation path is a file.");

            var token = Guid.NewGuid().ToString("N");
            var archivePath = Path.Combine(parentDirectory, ".webp-download-" + token);
            var tarPath = Path.Combine(parentDirectory, ".webp-tar-" + token);
            var stagingPath = Path.Combine(parentDirectory, ".webp-staging-" + token);
            var backupPath = Path.Combine(parentDirectory, ".webp-backup-" + token);
            var archiveIsZip = archiveName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase);

            try
            {
                using (var archiveFile = new FileStream(archivePath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                using (var sha256 = SHA256.Create())
                {
                    await CopyBoundedAsync(
                        archiveStream, archiveFile, MaxDownloadBytes, sha256, cancellationToken).ConfigureAwait(false);
                    var actualHash = ToLowerHex(sha256.Hash);
                    if (!string.Equals(actualHash, expectedSha256, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidDataException("The downloaded WebP archive does not match its pinned SHA-256 digest.");
                }

                Directory.CreateDirectory(stagingPath);
                if (archiveIsZip)
                {
                    ExtractZip(archivePath, stagingPath);
                }
                else
                {
                    using (var compressed = File.OpenRead(archivePath))
                    using (var gzip = new GZipStream(compressed, CompressionMode.Decompress))
                    using (var tarFile = new FileStream(tarPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                    {
                        await CopyBoundedAsync(
                            gzip, tarFile, MaxExpandedArchiveBytes, null, cancellationToken).ConfigureAwait(false);
                    }

                    TarFile.ExtractToDirectory(tarPath, stagingPath, Encoding.UTF8);
                }

                cancellationToken.ThrowIfCancellationRequested();
                ReplaceInstallation(stagingPath, installPath, backupPath);
            }
            finally
            {
                TryDeleteFile(archivePath);
                TryDeleteFile(tarPath);
                TryDeleteDirectory(stagingPath);
            }
        }

        /// <summary>
        /// Returns the selected archive details and executable paths for the current platform.
        /// </summary>
        private static (string url, string sha256, string encoderPath, string decoderPath) GetPlatformArchive()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var directory = Encoder.WebPEncoderBuilder._windowsDir;
                return (
                    WELL_KNOWN_WINDOWS_CLI_URL,
                    WindowsSha256,
                    Path.Combine(directory, "bin", "cwebp.exe"),
                    Path.Combine(directory, "bin", "dwebp.exe"));
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                var directory = Encoder.WebPEncoderBuilder._linuxDir;
                return (
                    WELL_KNOWN_LINUX_CLI_URL,
                    LinuxSha256,
                    Path.Combine(directory, "bin", "cwebp"),
                    Path.Combine(directory, "bin", "dwebp"));
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) && RuntimeInformation.ProcessArchitecture == Architecture.Arm64)
            {
                var directory = Encoder.WebPEncoderBuilder._osxARMDir;
                return (
                    WELL_KNOWN_OSX_ARM_CLI_URL,
                    MacArm64Sha256,
                    Path.Combine(directory, "bin", "cwebp"),
                    Path.Combine(directory, "bin", "dwebp"));
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                var directory = Encoder.WebPEncoderBuilder._osxDir;
                return (
                    WELL_KNOWN_OSX_CLI_URL,
                    MacX64Sha256,
                    Path.Combine(directory, "bin", "cwebp"),
                    Path.Combine(directory, "bin", "dwebp"));
            }

            throw new PlatformNotSupportedException();
        }

        /// <summary>
        /// Determines whether both current-version command-line executables are installed.
        /// </summary>
        /// <param name="installPath">Root directory where WebP CLI archives are extracted.</param>
        /// <returns>True only when both the current encoder and decoder executables exist.</returns>
        internal static bool IsCurrentInstallationComplete(string installPath)
        {
            var (_, _, encoderRelativePath, decoderRelativePath) = GetPlatformArchive();
            return IsCurrentInstallationComplete(installPath, encoderRelativePath, decoderRelativePath);
        }

        private static bool IsCurrentInstallationComplete(string installPath, string encoderRelativePath, string decoderRelativePath)
        {
            return File.Exists(Path.Combine(installPath, encoderRelativePath)) &&
                   File.Exists(Path.Combine(installPath, decoderRelativePath));
        }

        internal static Task<FileStream> AcquireInstallationLockAsync(string lockPath, CancellationToken cancellationToken)
        {
            return AcquireInstallationLockAsync(lockPath, cancellationToken, InstallationLockTimeout);
        }

        internal static async Task<FileStream> AcquireInstallationLockAsync(
            string lockPath,
            CancellationToken cancellationToken,
            TimeSpan timeout)
        {
            if (timeout <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(timeout), "The lock timeout must be positive.");

            var timer = Stopwatch.StartNew();
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (timer.Elapsed >= timeout)
                    throw new TimeoutException(
                        $"Could not acquire the WebP installation lock within {timeout.TotalSeconds:0.###} seconds.");
                try
                {
                    return new FileStream(lockPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
                }
                catch (IOException)
                {
                    var remaining = timeout - timer.Elapsed;
                    if (remaining <= TimeSpan.Zero)
                        throw new TimeoutException(
                            $"Could not acquire the WebP installation lock within {timeout.TotalSeconds:0.###} seconds.");

                    await Task.Delay(
                        remaining < InstallationLockRetryInterval ? remaining : InstallationLockRetryInterval,
                        cancellationToken).ConfigureAwait(false);
                }
            }
        }

        private static async Task<long> CopyBoundedAsync(
            Stream input,
            Stream output,
            long maximumBytes,
            SHA256 hash,
            CancellationToken cancellationToken)
        {
            var buffer = new byte[81920];
            long totalBytes = 0;
            int bytesRead;
            while ((bytesRead = await input.ReadAsync(
                buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false)) != 0)
            {
                if (totalBytes > maximumBytes - bytesRead)
                    throw new InvalidDataException("The WebP archive exceeds the configured size limit.");

                totalBytes += bytesRead;
                hash?.TransformBlock(buffer, 0, bytesRead, buffer, 0);
                await output.WriteAsync(buffer, 0, bytesRead, cancellationToken).ConfigureAwait(false);
            }

            if (hash != null)
                hash.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
            return totalBytes;
        }

        private static void ExtractZip(string archivePath, string stagingPath)
        {
            using var file = File.OpenRead(archivePath);
            using var archive = new ZipArchive(file, ZipArchiveMode.Read);
            long totalBytes = 0;
            var entryCount = 0;
            foreach (var entry in archive.Entries)
            {
                if (++entryCount > MaxEntries)
                    throw new InvalidDataException("The ZIP archive contains too many entries.");
                if (IsZipLink(entry))
                    throw new InvalidDataException("The ZIP archive contains a link entry.");
                if (entry.Length < 0 || entry.Length > MaxEntryBytes || totalBytes > MaxExpandedBytes - entry.Length)
                    throw new InvalidDataException("The ZIP archive exceeds the configured expanded size limit.");

                var outputPath = TarFile.ResolveEntryPath(stagingPath, entry.FullName);
                if (entry.FullName.EndsWith("/", StringComparison.Ordinal) ||
                    entry.FullName.EndsWith("\\", StringComparison.Ordinal))
                {
                    Directory.CreateDirectory(outputPath);
                    continue;
                }

                Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
                long copiedBytes;
                using (var input = entry.Open())
                using (var output = new FileStream(outputPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    copiedBytes = CopyBounded(input, output, entry.Length);
                }

                if (copiedBytes != entry.Length)
                    throw new InvalidDataException("The ZIP entry length does not match its contents.");
                totalBytes += copiedBytes;
            }
        }

        private static long CopyBounded(Stream input, Stream output, long maximumBytes)
        {
            var buffer = new byte[81920];
            long totalBytes = 0;
            int bytesRead;
            while ((bytesRead = input.Read(buffer, 0, buffer.Length)) != 0)
            {
                if (totalBytes > maximumBytes - bytesRead)
                    throw new InvalidDataException("An archive entry exceeds the configured size limit.");

                totalBytes += bytesRead;
                output.Write(buffer, 0, bytesRead);
            }

            return totalBytes;
        }

        private static bool IsZipLink(ZipArchiveEntry entry)
        {
            var unixFileType = (entry.ExternalAttributes >> 16) & 0xF000;
            return unixFileType == 0xA000 || (entry.ExternalAttributes & (int)FileAttributes.ReparsePoint) != 0;
        }

        private static void ReplaceInstallation(string stagingPath, string installPath, string backupPath)
        {
            var hadPreviousInstallation = Directory.Exists(installPath);
            if (hadPreviousInstallation)
                Directory.Move(installPath, backupPath);

            try
            {
                Directory.Move(stagingPath, installPath);
            }
            catch
            {
                if (hadPreviousInstallation && Directory.Exists(backupPath))
                    Directory.Move(backupPath, installPath);
                throw;
            }

            if (hadPreviousInstallation)
                Directory.Delete(backupPath, true);
        }

        private static string ToLowerHex(byte[] bytes)
        {
            var builder = new StringBuilder(bytes.Length * 2);
            foreach (var value in bytes)
                builder.Append(value.ToString("x2"));
            return builder.ToString();
        }

        private static void TryDeleteFile(string path)
        {
            try
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }

        private static void TryDeleteDirectory(string path)
        {
            try
            {
                if (Directory.Exists(path))
                    Directory.Delete(path, true);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }
}
