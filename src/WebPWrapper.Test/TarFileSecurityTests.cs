using System;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using WebPWrapper;
using Xunit;

namespace WebPWrapper.Test
{
    public class TarFileSecurityTests
    {
        [Fact]
        public void DefaultCliArchivesAndPathsUseLibwebp160()
        {
            Assert.Contains("libwebp-1.6.0", WebPExecuteDownloader.WELL_KNOWN_WINDOWS_CLI_URL);
            Assert.Contains("libwebp-1.6.0", WebPExecuteDownloader.WELL_KNOWN_LINUX_CLI_URL);
            Assert.Contains("libwebp-1.6.0", WebPExecuteDownloader.WELL_KNOWN_OSX_CLI_URL);
            Assert.Contains("libwebp-1.6.0", WebPExecuteDownloader.WELL_KNOWN_OSX_ARM_CLI_URL);
            Assert.Equal("libwebp-1.6.0-windows-x64", WebPWrapper.Encoder.WebPEncoderBuilder._windowsDir);
            Assert.Equal("libwebp-1.6.0-linux-x86-64", WebPWrapper.Encoder.WebPEncoderBuilder._linuxDir);
            Assert.Equal("libwebp-1.6.0-mac-x86-64", WebPWrapper.Encoder.WebPEncoderBuilder._osxDir);
            Assert.Equal("libwebp-1.6.0-mac-arm64", WebPWrapper.Encoder.WebPEncoderBuilder._osxARMDir);
            Assert.Equal("libwebp-1.6.0-windows-x64", WebPWrapper.Decoder.WebPDecoderBuilder._windowsDir);
            Assert.Equal("libwebp-1.6.0-linux-x86-64", WebPWrapper.Decoder.WebPDecoderBuilder._linuxDir);
            Assert.Equal("libwebp-1.6.0-mac-x86-64", WebPWrapper.Decoder.WebPDecoderBuilder._osxDir);
            Assert.Equal("libwebp-1.6.0-mac-arm64", WebPWrapper.Decoder.WebPDecoderBuilder._osxARMDir);
        }

        [Fact]
        public void DefaultInstallationUsesVersionedLocalApplicationDataPath()
        {
            var expected = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "WebPWrapper",
                "libwebp",
                "1.6.0");

            Assert.Equal(expected, WebPExecuteDownloader.GetDefaultInstallationPath());
        }

        [Fact]
        public async Task VersionedInstallationsCoexist()
        {
            using var temporaryDirectory = new TemporaryDirectory();
            var firstInstall = WebPExecuteDownloader.GetVersionedInstallationPath(temporaryDirectory.Path, "1.6.0");
            var secondInstall = WebPExecuteDownloader.GetVersionedInstallationPath(temporaryDirectory.Path, "1.7.0");
            var firstArchiveBytes = CreateGZip(CreateTar("libwebp/bin/cwebp", "version 1.6.0"));
            var secondArchiveBytes = CreateGZip(CreateTar("libwebp/bin/cwebp", "version 1.7.0"));

            using (var firstArchive = new MemoryStream(firstArchiveBytes))
            {
                await WebPExecuteDownloader.InstallArchiveAsync(
                    firstArchive, "libwebp-test.tar.gz", firstInstall, ComputeSha256(firstArchiveBytes));
            }

            using (var secondArchive = new MemoryStream(secondArchiveBytes))
            {
                await WebPExecuteDownloader.InstallArchiveAsync(
                    secondArchive, "libwebp-test.tar.gz", secondInstall, ComputeSha256(secondArchiveBytes));
            }

            Assert.Equal("version 1.6.0", File.ReadAllText(Path.Combine(firstInstall, "libwebp", "bin", "cwebp")));
            Assert.Equal("version 1.7.0", File.ReadAllText(Path.Combine(secondInstall, "libwebp", "bin", "cwebp")));
        }
        [Fact]
        public void ExistingLibwebp150InstallationDoesNotSatisfyCurrentVersionCheck()
        {
            using var temporaryDirectory = new TemporaryDirectory();
            var oldInstallations = new[]
            {
                "libwebp-1.5.0-windows-x64",
                "libwebp-1.5.0-linux-x86-64",
                "libwebp-1.5.0-mac-x86-64",
                "libwebp-1.5.0-mac-arm64"
            };

            foreach (var oldInstallation in oldInstallations)
            {
                var binDirectory = Path.Combine(temporaryDirectory.Path, oldInstallation, "bin");
                Directory.CreateDirectory(binDirectory);
                File.WriteAllText(Path.Combine(binDirectory, "cwebp"), "old encoder");
                File.WriteAllText(Path.Combine(binDirectory, "dwebp"), "old decoder");
            }

            Assert.False(WebPExecuteDownloader.IsCurrentInstallationComplete(temporaryDirectory.Path));
        }


        [Fact]
        public void ExtractToDirectory_RejectsPathTraversal()
        {
            using var temporaryDirectory = new TemporaryDirectory();
            var archivePath = Path.Combine(temporaryDirectory.Path, "traversal.tar");
            var destinationPath = Path.Combine(temporaryDirectory.Path, "extract");
            var escapedPath = Path.Combine(temporaryDirectory.Path, "escaped.txt");
            File.WriteAllBytes(archivePath, CreateTar("../escaped.txt", "untrusted"));

            Assert.Throws<InvalidDataException>(() =>
                TarFile.ExtractToDirectory(archivePath, destinationPath, null));
            Assert.False(File.Exists(escapedPath));
        }
        [Fact]
        public void ExtractToDirectory_RejectsEntryExceedingExpandedByteLimit()
        {
            using var temporaryDirectory = new TemporaryDirectory();
            using var archive = new MemoryStream(CreateTar("large.txt", "12345"));

            Assert.Throws<InvalidDataException>(() =>
                TarFile.ExtractToDirectory(archive, temporaryDirectory.Path, 4, 8, 10));
        }

        [Fact]
        public void ExtractToDirectory_RejectsSymbolicLinks()
        {
            using var temporaryDirectory = new TemporaryDirectory();
            using var archive = new MemoryStream(CreateTar("linked.txt", "", (byte)'2', "../escaped.txt"));

            Assert.Throws<InvalidDataException>(() =>
                TarFile.ExtractToDirectory(archive, temporaryDirectory.Path, 1024, 2048, 10));
        }
        [Fact]
        public async Task InstallArchiveAsync_RejectsHashMismatchWithoutReplacingExistingInstallation()
        {
            using var temporaryDirectory = new TemporaryDirectory();
            var installPath = Path.Combine(temporaryDirectory.Path, "webp");
            Directory.CreateDirectory(installPath);
            var existingFile = Path.Combine(installPath, "existing.txt");
            File.WriteAllText(existingFile, "existing installation");
            var archiveBytes = CreateGZip(CreateTar("bin/cwebp", "binary"));
            using var archive = new MemoryStream(archiveBytes);

            await Assert.ThrowsAsync<InvalidDataException>(() =>
                WebPExecuteDownloader.InstallArchiveAsync(
                    archive,
                    "libwebp-test.tar.gz",
                    installPath,
                    new string('0', 64)));

            Assert.Equal("existing installation", File.ReadAllText(existingFile));
        }
        [Fact]
        public async Task RecoverInterruptedInstallationAsync_RestoresIdentifiedBackupWithoutNetwork()
        {
            using var temporaryDirectory = new TemporaryDirectory();
            var installPath = Path.Combine(temporaryDirectory.Path, "webp");
            const string transactionId = "0123456789abcdef0123456789abcdef";
            var backupPath = Path.Combine(temporaryDirectory.Path, ".webp-backup-" + transactionId);
            Directory.CreateDirectory(Path.Combine(backupPath, "libwebp", "bin"));
            File.WriteAllText(Path.Combine(backupPath, "libwebp", "bin", "cwebp"), "previous encoder");
            File.WriteAllText(Path.Combine(backupPath, "libwebp", "bin", "dwebp"), "previous decoder");
            File.WriteAllText(
                WebPExecuteDownloader.GetInstallRecoveryStatePath(installPath, transactionId),
                Path.GetFullPath(installPath) + Environment.NewLine + "1");

            await WebPExecuteDownloader.RecoverInterruptedInstallationAsync(installPath);

            Assert.Equal("previous encoder", File.ReadAllText(Path.Combine(installPath, "libwebp", "bin", "cwebp")));
            Assert.Equal("previous decoder", File.ReadAllText(Path.Combine(installPath, "libwebp", "bin", "dwebp")));
            Assert.False(Directory.Exists(backupPath));
            Assert.False(File.Exists(WebPExecuteDownloader.GetInstallRecoveryStatePath(installPath, transactionId)));
        }

        [Fact]
        public async Task RecoverInterruptedInstallationAsync_LeavesUnidentifiedBackupUntouched()
        {
            using var temporaryDirectory = new TemporaryDirectory();
            var installPath = Path.Combine(temporaryDirectory.Path, "webp");
            const string transactionId = "fedcba9876543210fedcba9876543210";
            var backupPath = Path.Combine(temporaryDirectory.Path, ".webp-backup-" + transactionId);
            Directory.CreateDirectory(backupPath);
            File.WriteAllText(Path.Combine(backupPath, "owner.txt"), "unrelated backup");
            await WebPExecuteDownloader.RecoverInterruptedInstallationAsync(installPath);

            Assert.Equal("unrelated backup", File.ReadAllText(Path.Combine(backupPath, "owner.txt")));
        }
        [Fact]
        public async Task RecoverInterruptedInstallationAsync_LeavesAmbiguousBackupUntouched()
        {
            using var temporaryDirectory = new TemporaryDirectory();
            var installPath = Path.Combine(temporaryDirectory.Path, "webp");
            const string transactionId = "abcdef0123456789abcdef0123456789";
            var backupPath = Path.Combine(temporaryDirectory.Path, ".webp-backup-" + transactionId);
            Directory.CreateDirectory(installPath);
            File.WriteAllText(Path.Combine(installPath, "current.txt"), "current installation");
            Directory.CreateDirectory(backupPath);
            File.WriteAllText(Path.Combine(backupPath, "owner.txt"), "backup data");
            var statePath = WebPExecuteDownloader.GetInstallRecoveryStatePath(installPath, transactionId);
            File.WriteAllText(statePath, Path.GetFullPath(installPath) + Environment.NewLine + "1");

            await WebPExecuteDownloader.RecoverInterruptedInstallationAsync(installPath);

            Assert.Equal("current installation", File.ReadAllText(Path.Combine(installPath, "current.txt")));
            Assert.Equal("backup data", File.ReadAllText(Path.Combine(backupPath, "owner.txt")));
            Assert.True(File.Exists(statePath));
        }

        [Fact]
        public void ReplaceInstallation_FailedPromotionRestoresPreviousInstallation()
        {
            using var temporaryDirectory = new TemporaryDirectory();
            var installPath = Path.Combine(temporaryDirectory.Path, "webp");
            var stagingPath = Path.Combine(temporaryDirectory.Path, ".webp-staging-missing");
            const string transactionId = "00112233445566778899aabbccddeeff";
            var backupPath = Path.Combine(temporaryDirectory.Path, ".webp-backup-" + transactionId);
            Directory.CreateDirectory(Path.Combine(installPath, "libwebp", "bin"));
            File.WriteAllText(Path.Combine(installPath, "libwebp", "bin", "cwebp"), "previous encoder");
            File.WriteAllText(Path.Combine(installPath, "libwebp", "bin", "dwebp"), "previous decoder");

            Assert.Throws<DirectoryNotFoundException>(() =>
                WebPExecuteDownloader.ReplaceInstallation(stagingPath, installPath, backupPath));

            Assert.Equal("previous encoder", File.ReadAllText(Path.Combine(installPath, "libwebp", "bin", "cwebp")));
            Assert.Equal("previous decoder", File.ReadAllText(Path.Combine(installPath, "libwebp", "bin", "dwebp")));
            Assert.False(Directory.Exists(backupPath));
            Assert.False(File.Exists(WebPExecuteDownloader.GetInstallRecoveryStatePath(installPath, transactionId)));
        }
        [Fact]
        public void ReplaceInstallation_PreexistingBackupCollisionLeavesBothInstallationsUntouched()
        {
            using var temporaryDirectory = new TemporaryDirectory();
            var installPath = Path.Combine(temporaryDirectory.Path, "webp");
            var stagingPath = Path.Combine(temporaryDirectory.Path, ".webp-staging");
            const string transactionId = "8899aabbccddeeff0011223344556677";
            var backupPath = Path.Combine(temporaryDirectory.Path, ".webp-backup-" + transactionId);
            Directory.CreateDirectory(Path.Combine(installPath, "libwebp", "bin"));
            File.WriteAllText(Path.Combine(installPath, "libwebp", "bin", "cwebp"), "previous encoder");
            File.WriteAllText(Path.Combine(installPath, "libwebp", "bin", "dwebp"), "previous decoder");
            Directory.CreateDirectory(backupPath);
            File.WriteAllText(Path.Combine(backupPath, "owner.txt"), "unrelated backup");
            Directory.CreateDirectory(stagingPath);

            Assert.Throws<IOException>(() =>
                WebPExecuteDownloader.ReplaceInstallation(stagingPath, installPath, backupPath));

            Assert.Equal("previous encoder", File.ReadAllText(Path.Combine(installPath, "libwebp", "bin", "cwebp")));
            Assert.Equal("previous decoder", File.ReadAllText(Path.Combine(installPath, "libwebp", "bin", "dwebp")));
            Assert.Equal("unrelated backup", File.ReadAllText(Path.Combine(backupPath, "owner.txt")));
            Assert.False(File.Exists(WebPExecuteDownloader.GetInstallRecoveryStatePath(installPath, transactionId)));
        }


        [Fact]
        public async Task InstallArchiveAsync_RejectsZipPathTraversal()
        {
            using var temporaryDirectory = new TemporaryDirectory();
            var installPath = Path.Combine(temporaryDirectory.Path, "webp");
            var escapedPath = Path.Combine(temporaryDirectory.Path, "escaped.txt");
            var archiveBytes = CreateZip("../escaped.txt", "untrusted");
            using var archive = new MemoryStream(archiveBytes);
            var digest = ComputeSha256(archiveBytes);

            await Assert.ThrowsAsync<InvalidDataException>(() =>
                WebPExecuteDownloader.InstallArchiveAsync(
                    archive,
                    "libwebp-test.zip",
                    installPath,
                    digest));

            Assert.False(File.Exists(escapedPath));
            Assert.False(Directory.Exists(installPath));
        }

        private static byte[] CreateZip(string entryName, string contents)
        {
            using var output = new MemoryStream();
            using (var archive = new ZipArchive(output, ZipArchiveMode.Create, true))
            using (var entry = archive.CreateEntry(entryName).Open())
            {
                var bytes = Encoding.UTF8.GetBytes(contents);
                entry.Write(bytes, 0, bytes.Length);
            }

            return output.ToArray();
        }

        private static string ComputeSha256(byte[] content)
        {
            using var sha256 = SHA256.Create();
            var digest = sha256.ComputeHash(content);
            return BitConverter.ToString(digest).Replace("-", string.Empty);
        }

        private static byte[] CreateGZip(byte[] content)
        {
            using var output = new MemoryStream();
            using (var gzip = new GZipStream(output, CompressionLevel.Optimal, true))
            {
                gzip.Write(content, 0, content.Length);
            }

            return output.ToArray();
        }
        [Fact]
        public async Task InstallArchiveAsync_VerifiesAndInstallsValidTarArchive()
        {
            using var temporaryDirectory = new TemporaryDirectory();
            var installPath = Path.Combine(temporaryDirectory.Path, "webp");
            var archiveBytes = CreateGZip(CreateTar("libwebp/bin/cwebp", "verified binary"));
            using var archive = new MemoryStream(archiveBytes);

            await WebPExecuteDownloader.InstallArchiveAsync(
                archive,
                "libwebp-test.tar.gz",
                installPath,
                ComputeSha256(archiveBytes));

            Assert.Equal("verified binary", File.ReadAllText(Path.Combine(installPath, "libwebp", "bin", "cwebp")));
        }




        private static byte[] CreateTar(string entryName, string contents, byte type = (byte)'0', string linkName = "")
        {
            var data = Encoding.UTF8.GetBytes(contents);
            var archive = new byte[1024 + ((data.Length + 511) / 512 * 512)];
            var header = archive;
            Encoding.ASCII.GetBytes(entryName).CopyTo(header, 0);
            WriteOctal(header, 100, 8, 0x1A4);
            WriteOctal(header, 108, 8, 0);
            WriteOctal(header, 116, 8, 0);
            WriteOctal(header, 124, 12, data.Length);
            Encoding.ASCII.GetBytes(linkName).CopyTo(header, 157);
            header[156] = type;
            Encoding.ASCII.GetBytes("ustar\0").CopyTo(header, 257);
            WriteOctal(header, 148, 8, Checksum(header));
            data.CopyTo(archive, 512);
            return archive;
        }

        private static int Checksum(byte[] header)
        {
            var sum = 0;
            for (var index = 0; index < 512; index++)
            {
                sum += index >= 148 && index < 156 ? 32 : header[index];
            }

            return sum;
        }

        private static void WriteOctal(byte[] buffer, int offset, int length, long value)
        {
            var text = Convert.ToString(value, 8).PadLeft(length - 1, '0');
            Encoding.ASCII.GetBytes(text).CopyTo(buffer, offset + length - text.Length - 1);
            buffer[offset + length - 1] = 0;
        }

        private sealed class TemporaryDirectory : IDisposable
        {
            public TemporaryDirectory()
            {
                Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(Path);
            }

            public string Path { get; }

            public void Dispose()
            {
                Directory.Delete(Path, true);
            }
        }
    }
}
