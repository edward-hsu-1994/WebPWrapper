using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace WebPWrapper
{
    /// <summary>
    /// Tar Helper
    /// </summary>
    internal class TarFile
    {
        public static void ExtractToDirectory(
            string sourceArchiveFileName,
            string destinationDirectoryName,
            Encoding? entryNameEncoding)
        {
            using var stream = File.Open(sourceArchiveFileName, FileMode.Open);

            var buffer = new byte[100];
            while (true)
            {
                // load filename
                stream.Read(buffer, 0, 100);
                var name = Encoding.ASCII.GetString(buffer).Trim('\0');

                if (string.IsNullOrWhiteSpace(name))break;

                // skip file mode, owner user ID, owner group ID
                stream.Seek(8 * 3, SeekOrigin.Current);

                // load filesize
                stream.Read(buffer, 0, 12);
                
                var size = Convert.ToInt64(Encoding.UTF8.GetString(buffer, 0, 12).Trim('\0').Trim(), 8);

                stream.Seek(376L, SeekOrigin.Current);

                var output = Path.Combine(destinationDirectoryName, name);
                if (!Directory.Exists(Path.GetDirectoryName(output)))
                    Directory.CreateDirectory(Path.GetDirectoryName(output));
                if (!name.EndsWith("/", StringComparison.InvariantCulture))
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
    }
}
