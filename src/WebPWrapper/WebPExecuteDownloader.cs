using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
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
        private const long MaxEntryBytes = 128L * 1024 * 1024;
        private const long MaxExpandedBytes = 256L * 1024 * 1024;
        private const int MaxEntries = 4096;
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
        public static async Task DownloadAsync(bool ignoreIfExtsis = true)
        {
            var (downloadUrl, expectedSha256, encoderRelativePath, decoderRelativePath) = GetPlatformArchive();
            var installPath = GetDefaultInstallationPath();
            var installParent = Path.GetDirectoryName(installPath);
            Directory.CreateDirectory(installParent);

            using (await AcquireInstallationLockAsync(Path.Combine(installParent, ".webp.install.lock")).ConfigureAwait(false))
            {
                RecoverInterruptedInstallation(installPath);
                if (ignoreIfExtsis && IsCurrentInstallationComplete(installPath, encoderRelativePath, decoderRelativePath))
                    return;

                using (var response = await HttpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false))
                {
                    response.EnsureSuccessStatusCode();
                    using (var responseStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                    {
                        await InstallArchiveAsync(responseStream, downloadUrl, installPath, expectedSha256).ConfigureAwait(false);
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

        /// <summary>
        /// Stages an archive, checks its pinned digest, safely extracts it, and replaces the installation.
        /// </summary>
        /// <param name="archiveStream">Readable stream containing the downloaded archive.</param>
        /// <param name="archiveName">Fixed release archive URL or name, used to select ZIP versus TAR.GZ.</param>
        /// <param name="installPath">Destination installation directory.</param>
        /// <param name="expectedSha256">Trusted lowercase or uppercase SHA-256 digest.</param>
        /// <exception cref="InvalidDataException">The archive exceeds limits or has a digest mismatch.</exception>
        internal static async Task InstallArchiveAsync(
            Stream archiveStream,
            string archiveName,
            string installPath,
            string expectedSha256)
        {
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
                    await CopyBoundedAsync(archiveStream, archiveFile, MaxDownloadBytes, sha256).ConfigureAwait(false);
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
                        await CopyBoundedAsync(gzip, tarFile, MaxExpandedArchiveBytes).ConfigureAwait(false);
                    }

                    TarFile.ExtractToDirectory(tarPath, stagingPath, Encoding.UTF8);
                }

                ReplaceInstallation(stagingPath, installPath, backupPath);
            }
            finally
            {
                TryDeleteFile(archivePath);
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

        private static async Task<FileStream> AcquireInstallationLockAsync(string lockPath)
        {
            var timer = Stopwatch.StartNew();
            while (true)
            {
                try
                {
                    return new FileStream(lockPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
                }
                catch (IOException) when (timer.Elapsed < TimeSpan.FromSeconds(30))
                {
                    await Task.Delay(100).ConfigureAwait(false);
                }
            }
        }

        private static async Task<long> CopyBoundedAsync(
            Stream input,
            Stream output,
            long maximumBytes,
            SHA256 hash = null)
        {
            var buffer = new byte[81920];
            long totalBytes = 0;
            int bytesRead;
            while ((bytesRead = await input.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false)) != 0)
            {
                if (totalBytes > maximumBytes - bytesRead)
                    throw new InvalidDataException("The WebP archive exceeds the configured size limit.");

                totalBytes += bytesRead;
                hash?.TransformBlock(buffer, 0, bytesRead, buffer, 0);
                await output.WriteAsync(buffer, 0, bytesRead).ConfigureAwait(false);
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

        internal static async Task RecoverInterruptedInstallationAsync(string installPath)
        {
            installPath = Path.GetFullPath(installPath);
            var parentDirectory = Path.GetDirectoryName(installPath);
            Directory.CreateDirectory(parentDirectory);
            using (await AcquireInstallationLockAsync(Path.Combine(parentDirectory, ".webp.install.lock")).ConfigureAwait(false))
            {
                RecoverInterruptedInstallation(installPath);
            }
        }

        private static void RecoverInterruptedInstallation(string installPath)
        {
            var parentDirectory = Path.GetDirectoryName(installPath);
            foreach (var statePath in Directory.GetFiles(parentDirectory, ".webp-install-*.state"))
            {
                var fileName = Path.GetFileName(statePath);
                var transactionId = fileName.Substring(".webp-install-".Length, fileName.Length - ".webp-install-".Length - ".state".Length);
                if (!Guid.TryParseExact(transactionId, "N", out _))
                    continue;

                string[] stateLines;
                try
                {
                    stateLines = File.ReadAllLines(statePath);
                }
                catch (IOException)
                {
                    continue;
                }
                catch (UnauthorizedAccessException)
                {
                    continue;
                }

                if (stateLines.Length != 2 ||
                    !string.Equals(stateLines[0], installPath, StringComparison.Ordinal) ||
                    (stateLines[1] != "0" && stateLines[1] != "1"))
                    continue;

                var backupPath = Path.Combine(parentDirectory, ".webp-backup-" + transactionId);
                var hadPreviousInstallation = stateLines[1] == "1";
                if (!Directory.Exists(installPath) && hadPreviousInstallation && Directory.Exists(backupPath))
                {
                    Directory.Move(backupPath, installPath);
                    TryDeleteFile(statePath);
                }
                else if (!Directory.Exists(backupPath))
                {
                    TryDeleteFile(statePath);
                }
                // When both paths exist, ownership of the backup is ambiguous; leave both untouched.
            }
        }

        internal static string GetInstallRecoveryStatePath(string installPath, string transactionId)
        {
            return Path.Combine(Path.GetDirectoryName(Path.GetFullPath(installPath)), ".webp-install-" + transactionId + ".state");
        }

        internal static void ReplaceInstallation(string stagingPath, string installPath, string backupPath)
        {
            var hadPreviousInstallation = Directory.Exists(installPath);
            if (Directory.Exists(backupPath) || File.Exists(backupPath))
                throw new IOException("A WebP installation backup path already exists.");

            var transactionId = Path.GetFileName(backupPath).Substring(".webp-backup-".Length);
            var statePath = GetInstallRecoveryStatePath(installPath, transactionId);
            using (var state = new FileStream(statePath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            using (var writer = new StreamWriter(state))
            {
                writer.Write(Path.GetFullPath(installPath));
                writer.WriteLine();
                writer.Write(hadPreviousInstallation ? "1" : "0");
            }
            if (hadPreviousInstallation)
                Directory.Move(installPath, backupPath);

            try
            {
                Directory.Move(stagingPath, installPath);
            }
            catch
            {
                if (hadPreviousInstallation && Directory.Exists(backupPath))
                {
                    Directory.Move(backupPath, installPath);
                    TryDeleteFile(statePath);
                }
                throw;
            }

            if (hadPreviousInstallation)
                Directory.Delete(backupPath, true);
            TryDeleteFile(statePath);
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
