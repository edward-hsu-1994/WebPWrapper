using System;
using System.IO;
using Xunit;

namespace WebPWrapper.Test
{
    public class WebPExecuteDownloadHelper : IDisposable
    {
        private readonly string _previousDirectory;
        private readonly string _downloadDirectory;

        public WebPExecuteDownloadHelper()
        {
            _previousDirectory = Environment.CurrentDirectory;
            _downloadDirectory = Path.Combine(Path.GetTempPath(), "WebPWrapper-Integration-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_downloadDirectory);
            try
            {
                var sampleSourceDirectory = Path.Combine(AppContext.BaseDirectory, "Samples");
                var sampleDirectory = Path.Combine(_downloadDirectory, "Samples");
                Directory.CreateDirectory(sampleDirectory);
                foreach (var samplePath in Directory.GetFiles(sampleSourceDirectory))
                    File.Copy(samplePath, Path.Combine(sampleDirectory, Path.GetFileName(samplePath)));
                Environment.CurrentDirectory = _downloadDirectory;
                WebPExecuteDownloader.Download();
            }
            catch
            {
                Environment.CurrentDirectory = _previousDirectory;
                Directory.Delete(_downloadDirectory, true);
                throw;
            }
        }

        public void Dispose()
        {
            Environment.CurrentDirectory = _previousDirectory;
            if (Directory.Exists(_downloadDirectory))
                Directory.Delete(_downloadDirectory, true);
        }
    }

    [CollectionDefinition("WebP Init")]
    public class WebPIntegrationCollection : ICollectionFixture<WebPExecuteDownloadHelper>
    {
    }

    internal sealed class TemporaryTestDirectory : IDisposable
    {
        public TemporaryTestDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "WebPWrapper-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public string OutputPath(string fileName) => System.IO.Path.Combine(Path, fileName);

        public void Dispose()
        {
            if (Directory.Exists(Path))
                Directory.Delete(Path, true);
        }
    }
}
