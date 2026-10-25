using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using WebPWrapper;
using Xunit;

namespace WebPWrapper.Test
{
    public class DownloadCancellationTests
    {
        [Fact]
        public async Task AcquireInstallationLockAsync_ThrowsMeaningfulTimeoutWhenLockRemainsHeld()
        {
            using var directory = new TemporaryTestDirectory();
            var lockPath = directory.OutputPath("install.lock");
            using var heldLock = new FileStream(lockPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);

            var exception = await Assert.ThrowsAsync<TimeoutException>(() =>
                WebPExecuteDownloader.AcquireInstallationLockAsync(
                    lockPath, CancellationToken.None, TimeSpan.FromMilliseconds(250)));

            Assert.Contains("installation lock", exception.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("0.25 seconds", exception.Message);
        }

        [Fact]
        public async Task AcquireInstallationLockAsync_ObservesCancellationWhileWaiting()
        {
            using var directory = new TemporaryTestDirectory();
            var lockPath = directory.OutputPath("install.lock");
            using var heldLock = new FileStream(lockPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            using var cancellation = new CancellationTokenSource();
            cancellation.CancelAfter(TimeSpan.FromMilliseconds(50));

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                WebPExecuteDownloader.AcquireInstallationLockAsync(
                    lockPath, cancellation.Token, TimeSpan.FromSeconds(5)));
        }

        [Fact]
        public async Task InstallArchiveAsync_CancelsBlockedResponseBodyRead()
        {
            using var directory = new TemporaryTestDirectory();
            using var cancellation = new CancellationTokenSource();
            using var body = new BlockingReadStream();
            var installationPath = directory.OutputPath("install");
            var installTask = WebPExecuteDownloader.InstallArchiveAsync(
                body,
                "libwebp-test.tar.gz",
                installationPath,
                new string('0', 64),
                cancellation.Token);

            await body.ReadStarted;
            cancellation.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => installTask);
            Assert.False(Directory.Exists(installationPath));
        }

        private sealed class BlockingReadStream : Stream
        {
            private readonly TaskCompletionSource<bool> _readStarted =
                new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            public Task ReadStarted => _readStarted.Task;
            public override bool CanRead => true;
            public override bool CanSeek => false;
            public override bool CanWrite => false;
            public override long Length => throw new NotSupportedException();
            public override long Position
            {
                get => throw new NotSupportedException();
                set => throw new NotSupportedException();
            }

            public override Task<int> ReadAsync(
                byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                _readStarted.TrySetResult(true);
                return WaitForCancellationAsync(cancellationToken);
            }

            private static async Task<int> WaitForCancellationAsync(CancellationToken cancellationToken)
            {
                await Task.Delay(Timeout.Infinite, cancellationToken);
                return 0;
            }

            public override int Read(byte[] buffer, int offset, int count) =>
                throw new NotSupportedException();
            public override void Flush() => throw new NotSupportedException();
            public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
            public override void SetLength(long value) => throw new NotSupportedException();
            public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        }
    }
}
