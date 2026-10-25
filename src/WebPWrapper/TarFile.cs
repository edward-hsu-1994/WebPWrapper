using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using ICSharpCode.SharpZipLib.Tar;

namespace WebPWrapper
{
    /// <summary>
    /// Extracts TAR entries into a staging directory while rejecting unsafe
    /// paths, links, unsupported entry types, and archives exceeding limits.
    /// </summary>
    internal static class TarFile
    {
        private const int ExecutableMode = 0x1ED;
        private const int ExecuteBits = 0x49;

        /// <summary>
        /// Extracts a TAR file using the default archive safety limits.
        /// </summary>
        /// <param name="sourceArchiveFileName">Path to the TAR archive.</param>
        /// <param name="destinationDirectoryName">Empty directory for extracted entries.</param>
        /// <param name="entryNameEncoding">Encoding used to decode entry names.</param>
        public static void ExtractToDirectory(
            string sourceArchiveFileName,
            string destinationDirectoryName,
            Encoding entryNameEncoding)
        {
            using var stream = File.OpenRead(sourceArchiveFileName);
            ExtractToDirectory(stream, destinationDirectoryName, 128L * 1024 * 1024, 256L * 1024 * 1024, 4096, entryNameEncoding);
        }

        /// <summary>
        /// Extracts TAR entries after validating entry type, path, and declared size.
        /// </summary>
        /// <param name="archiveStream">Readable stream positioned at a TAR archive.</param>
        /// <param name="destinationDirectoryName">Empty directory for extracted entries.</param>
        /// <param name="maxEntryBytes">Maximum uncompressed bytes for one entry.</param>
        /// <param name="maxTotalBytes">Maximum uncompressed bytes across all entries.</param>
        /// <param name="maxEntries">Maximum number of archive entries.</param>
        /// <param name="entryNameEncoding">Encoding used to decode entry names.</param>
        /// <exception cref="InvalidDataException">The archive contains unsafe or oversized entries.</exception>
        public static void ExtractToDirectory(
            Stream archiveStream,
            string destinationDirectoryName,
            long maxEntryBytes,
            long maxTotalBytes,
            int maxEntries,
            Encoding entryNameEncoding = null)
        {
            if (archiveStream == null)
                throw new ArgumentNullException(nameof(archiveStream));
            if (destinationDirectoryName == null)
                throw new ArgumentNullException(nameof(destinationDirectoryName));
            if (maxEntryBytes <= 0 || maxTotalBytes <= 0 || maxEntries <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxEntryBytes), "Archive extraction limits must be positive.");

            var destinationRoot = Path.GetFullPath(destinationDirectoryName);
            Directory.CreateDirectory(destinationRoot);
            long totalBytes = 0;
            var entryCount = 0;

            using var tarStream = new TarInputStream(archiveStream, entryNameEncoding ?? Encoding.UTF8);
            tarStream.IsStreamOwner = false;
            TarEntry entry;
            while ((entry = tarStream.GetNextEntry()) != null)
            {
                if (++entryCount > maxEntries)
                    throw new InvalidDataException("The TAR archive contains too many entries.");

                var type = entry.TarHeader.TypeFlag;
                var isDirectory = type == TarHeader.LF_DIR;
                if (!isDirectory && type != TarHeader.LF_NORMAL && type != TarHeader.LF_OLDNORM)
                    throw new InvalidDataException("The TAR archive contains an unsupported entry type.");

                if (entry.Size < 0 || entry.Size > maxEntryBytes || (isDirectory && entry.Size != 0))
                    throw new InvalidDataException("A TAR entry exceeds the configured size limit.");

                try
                {
                    totalBytes = checked(totalBytes + entry.Size);
                }
                catch (OverflowException exception)
                {
                    throw new InvalidDataException("The TAR archive exceeds the configured size limit.", exception);
                }

                if (totalBytes > maxTotalBytes)
                    throw new InvalidDataException("The TAR archive exceeds the configured size limit.");

                var outputPath = ResolveEntryPath(destinationRoot, entry.Name);
                if (isDirectory)
                {
                    Directory.CreateDirectory(outputPath);
                    continue;
                }

                var parentDirectory = Path.GetDirectoryName(outputPath);
                Directory.CreateDirectory(parentDirectory);
                using (var output = new FileStream(outputPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    tarStream.CopyEntryContents(output);
                }

                if ((entry.TarHeader.Mode & ExecuteBits) != 0 && !RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    SetExecutablePermission(outputPath);
            }
        }

        /// <summary>
        /// Resolves an archive path beneath the extraction root and rejects rooted or escaping paths.
        /// </summary>
        internal static string ResolveEntryPath(string root, string entryName)
        {
            var destinationRoot = Path.GetFullPath(root);
            var rootPrefix = destinationRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                + Path.DirectorySeparatorChar;
            if (string.IsNullOrWhiteSpace(entryName) ||
                entryName.StartsWith("/", StringComparison.Ordinal) ||
                entryName.StartsWith("\\", StringComparison.Ordinal) ||
                entryName.IndexOf(':') >= 0)
            {
                throw new InvalidDataException("The archive contains a rooted or empty entry path.");
            }

            var normalizedName = entryName.Replace('\\', '/');
            foreach (var segment in normalizedName.Split('/'))
            {
                if (segment == "..")
                    throw new InvalidDataException("The archive contains a path traversal entry.");
            }

            var outputPath = Path.GetFullPath(Path.Combine(
                destinationRoot,
                normalizedName.Replace('/', Path.DirectorySeparatorChar)));
            var comparison = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal;
            if (!outputPath.StartsWith(rootPrefix, comparison))
                throw new InvalidDataException("The archive entry escapes the extraction directory.");

            return outputPath;
        }

        /// <summary>
        /// Marks an archive-declared executable file as owner/group/world executable.
        /// </summary>
        private static void SetExecutablePermission(string path)
        {
            if (Chmod(path, ExecutableMode) != 0)
                throw new IOException("Could not set executable permissions on an extracted WebP binary.");
        }

        [DllImport("libc", EntryPoint = "chmod", SetLastError = true, CharSet = CharSet.Ansi)]
        private static extern int Chmod(string path, int mode);
    }
}
