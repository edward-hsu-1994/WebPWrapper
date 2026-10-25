using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WebPWrapper;
using WebPWrapper.Decoder;
using WebPWrapper.Encoder;
using Xunit;

namespace WebPWrapper.Test {
    public class AsyncProcessRunnerTests {
        [Fact]
        public async Task EncodeAsync_CopiesOutputAndRemovesTemporaryFiles() {
            if (!IsUnix()) return;
            using (var directory = new TestDirectory()) {
                var scriptPath = Path.Combine(directory.Path, "fake webp cli");
                var recordPath = Path.Combine(directory.Path, "paths.txt");
                File.WriteAllText(scriptPath, "#!/bin/sh\nprintf 'encoded data' > \"$2\"\nprintf '%s\\n%s' \"$2\" \"$3\" > '" + recordPath.Replace("'", "'\\''") + "'\n");
                MakeExecutable(scriptPath);
                var output = new MemoryStream();
                await new WebPEncoder(scriptPath, "").EncodeAsync(new MemoryStream(Encoding.UTF8.GetBytes("input")), output);
                Assert.Equal("encoded data", Encoding.UTF8.GetString(output.ToArray()));
                AssertTemporaryFilesRemoved(recordPath);
            }
        }

        [Fact]
        public async Task DecodeAsync_CopiesOutputAndRemovesTemporaryFiles() {
            if (!IsUnix()) return;
            using (var directory = new TestDirectory()) {
                var scriptPath = Path.Combine(directory.Path, "fake webp cli");
                var recordPath = Path.Combine(directory.Path, "paths.txt");
                File.WriteAllText(scriptPath, "#!/bin/sh\nprintf 'decoded data' > \"$2\"\nprintf '%s\\n%s' \"$2\" \"$3\" > '" + recordPath.Replace("'", "'\\''") + "'\n");
                MakeExecutable(scriptPath);
                var output = new MemoryStream();
                await new WebPDecoder(scriptPath, "").DecodeAsync(new MemoryStream(Encoding.UTF8.GetBytes("input")), output);
                Assert.Equal("decoded data", Encoding.UTF8.GetString(output.ToArray()));
                AssertTemporaryFilesRemoved(recordPath);
            }
        }

        [Fact]
        public async Task RunAsync_CapturesStandardErrorCompletelyOnFailure() {
            if (!IsUnix()) return;
            const int bytes = 250000;
            using (var script = new TestScript("printf 'out'; printf '%0250000d' 0 >&2; exit 17")) {
                var exception = await Assert.ThrowsAsync<ProcessExecutionException>(() => ProcessRunner.RunAsync("/bin/sh", new[] { "-c", script.Body }, TimeSpan.FromSeconds(10), CancellationToken.None));
                Assert.Equal(ProcessFailureKind.NonZeroExit, exception.Kind);
                Assert.Equal(17, exception.ExitCode);
                Assert.Equal("out", exception.StandardOutput.TrimEnd('\r', '\n'));
                Assert.Equal(bytes, exception.StandardError.TrimEnd('\r', '\n').Length);
            }
        }

        [Fact]
        public async Task RunAsync_TerminatesTimedOutProcess() {
            if (!IsUnix()) return;
            using (var script = new TestScript("exec sleep 30")) {
                var exception = await Assert.ThrowsAsync<ProcessExecutionException>(() => ProcessRunner.RunAsync("/bin/sh", new[] { "-c", script.Body }, TimeSpan.FromMilliseconds(150), CancellationToken.None));
                Assert.Equal(ProcessFailureKind.TimedOut, exception.Kind);
                Assert.NotNull(exception.ExitCode);
            }
        }

        [Fact]
        public async Task EncodeAsync_CancellationTerminatesProcessAndRemovesTemporaryFiles() {
            if (!IsUnix()) return;
            using (var directory = new TestDirectory()) {
                var scriptPath = Path.Combine(directory.Path, "fake webp cli");
                var recordPath = Path.Combine(directory.Path, "paths.txt");
                File.WriteAllText(scriptPath, "#!/bin/sh\nprintf '%s\\n%s' \"$2\" \"$3\" > '" + recordPath.Replace("'", "'\\''") + "'\nexec sleep 30\n");
                MakeExecutable(scriptPath);
                using (var cancellation = new CancellationTokenSource()) {
                    var operation = new WebPEncoder(scriptPath, "").EncodeAsync(new MemoryStream(Encoding.UTF8.GetBytes("input")), new MemoryStream(), cancellation.Token);
                    var deadline = DateTime.UtcNow.AddSeconds(10);
                    while (!File.Exists(recordPath) && DateTime.UtcNow < deadline) await Task.Delay(20);
                    Assert.True(File.Exists(recordPath), "The fake CLI did not start.");
                    cancellation.Cancel();
                    var exception = await Assert.ThrowsAsync<ProcessExecutionCanceledException>(() => operation);
                    Assert.Equal(ProcessFailureKind.Canceled, exception.FailureDetails.Kind);
                }
                AssertTemporaryFilesRemoved(recordPath);
            }
        }

        private static void AssertTemporaryFilesRemoved(string recordPath) {
            var paths = File.ReadAllLines(recordPath);
            Assert.Equal(2, paths.Length);
            Assert.All(paths, path => Assert.False(File.Exists(path)));
        }

        private static bool IsUnix() => Path.DirectorySeparatorChar == '/';

        private static void MakeExecutable(string path) {
            using (var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo {
                FileName = "/bin/chmod",
                Arguments = "+x \"" + path + "\"",
                UseShellExecute = false
            })) process.WaitForExit();
        }

        private sealed class TestScript : IDisposable {
            internal TestScript(string body) { Body = body; }
            internal string Body { get; }
            public void Dispose() { }
        }

        private sealed class TestDirectory : IDisposable {
            internal TestDirectory() {
                Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "WebP async process runner " + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(Path);
            }
            internal string Path { get; }
            public void Dispose() {
                if (Directory.Exists(Path)) Directory.Delete(Path, true);
            }
        }
    }
}
