using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using WebPWrapper;
using WebPWrapper.Encoder;
using Xunit;

namespace WebPWrapper.Test {
    public class ProcessRunnerTests {
        [Fact]
        public void Run_PreservesArgumentWithSpacesWithoutShellInterpretation() {
            if (!IsUnix()) return;
            using (var script = new TestScript("printf '%s' \"$1\"")) {
                var value = "a path with spaces; touch SHOULD_NOT_EXIST";
                var result = ProcessRunner.Run("/bin/sh", new[] { "-c", script.Body, "test", value }, TimeSpan.FromSeconds(5), CancellationToken.None);
                Assert.Equal(value, result.StandardOutput.TrimEnd('\r', '\n'));
                Assert.False(File.Exists("SHOULD_NOT_EXIST"));
            }
        }

        [Fact]
        public void Run_NonzeroExitIncludesCompleteSeparateOutputAndError() {
            if (!IsUnix()) return;
            using (var script = new TestScript("printf 'out'; printf 'err' >&2; exit 17")) {
                var exception = Assert.Throws<ProcessExecutionException>(() => ProcessRunner.Run("/bin/sh", new[] { "-c", script.Body }, TimeSpan.FromSeconds(5), CancellationToken.None));
                Assert.Equal(ProcessFailureKind.NonZeroExit, exception.Kind);
                Assert.Equal(17, exception.ExitCode);
                Assert.Equal("out", exception.StandardOutput.TrimEnd('\r', '\n'));
                Assert.Equal("err", exception.StandardError.TrimEnd('\r', '\n'));
            }
        }

        [Fact]
        public void Run_DrainsLargeStandardErrorWithoutTruncation() {
            if (!IsUnix()) return;
            const int bytes = 250000;
            using (var script = new TestScript("printf '%0250000d' 0 >&2; exit 1")) {
                var exception = Assert.Throws<ProcessExecutionException>(() => ProcessRunner.Run("/bin/sh", new[] { "-c", script.Body, "large" }, TimeSpan.FromSeconds(10), CancellationToken.None));
                Assert.Equal(bytes, exception.StandardError.TrimEnd('\r', '\n').Length);
            }
        }

        [Fact]
        public void Run_TerminatesTimedOutProcess() {
            if (!IsUnix()) return;
            using (var script = new TestScript("exec sleep 30")) {
                var exception = Assert.Throws<ProcessExecutionException>(() => ProcessRunner.Run("/bin/sh", new[] { "-c", script.Body }, TimeSpan.FromMilliseconds(150), CancellationToken.None));
                Assert.Equal(ProcessFailureKind.TimedOut, exception.Kind);
                Assert.NotNull(exception.ExitCode);
            }
        }

        [Fact]
        public void Run_TerminatesCanceledProcess() {
            if (!IsUnix()) return;
            using (var script = new TestScript("exec sleep 30"))
            using (var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(150))) {
                var exception = Assert.Throws<ProcessExecutionCanceledException>(() => ProcessRunner.Run("/bin/sh", new[] { "-c", script.Body }, TimeSpan.FromSeconds(10), cancellation.Token));
                Assert.Equal(ProcessFailureKind.Canceled, exception.FailureDetails.Kind);
            }
        }

        [Fact]
        public void Encode_SuccessCopiesOutputAndRemovesTemporaryFiles() {
            if (!IsUnix()) return;
            using (var directory = new TestDirectory()) {
                var scriptPath = Path.Combine(directory.Path, "fake webp cli");
                var recordPath = Path.Combine(directory.Path, "paths.txt");
                File.WriteAllText(scriptPath, "#!/bin/sh\nprintf 'encoded data' > \"$2\"\nprintf '%s\\n%s' \"$2\" \"$3\" > '" + recordPath.Replace("'", "'\\''") + "'\n");
                MakeExecutable(scriptPath);
                var output = new MemoryStream();
                var encoder = new WebPEncoder(scriptPath, "");
                encoder.Encode(new MemoryStream(Encoding.UTF8.GetBytes("input")), output);
                Assert.Equal("encoded data", Encoding.UTF8.GetString(output.ToArray()));
                var paths = File.ReadAllLines(recordPath);
                Assert.Equal(2, paths.Length);
                Assert.All(paths, path => Assert.False(File.Exists(path)));
            }
        }

        private static bool IsUnix() => Path.DirectorySeparatorChar == '/';

        private static void MakeExecutable(string path) {
            using (var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo {
                FileName = "/bin/chmod",
                Arguments = "+x \"" + path.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"",
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
                Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "WebP process runner " + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(Path);
            }
            internal string Path { get; }
            public void Dispose() {
                if (Directory.Exists(Path)) Directory.Delete(Path, true);
            }
        }
    }
}
