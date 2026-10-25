using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace WebPWrapper {
    /// <summary>Identifies why a WebP CLI process execution failed.</summary>
    public enum ProcessFailureKind {
        /// <summary>The CLI executable could not be started.</summary>
        StartFailed,
        /// <summary>The CLI process exited with a nonzero status code.</summary>
        NonZeroExit,
        /// <summary>The CLI process exceeded its finite execution timeout.</summary>
        TimedOut,
        /// <summary>The operation was canceled and the CLI process was terminated.</summary>
        Canceled
    }

    /// <summary>Describes a failed external process execution.</summary>
    public sealed class ProcessExecutionException : Exception {
        internal ProcessExecutionException(string message, ProcessFailureKind kind, string executablePath, int? exitCode, string standardOutput, string standardError, Exception innerException = null)
            : base(message, innerException) {
            Kind = kind;
            ExecutablePath = executablePath;
            ExitCode = exitCode;
            StandardOutput = standardOutput;
            StandardError = standardError;
        }

        /// <summary>Gets the failure category: StartFailed, NonZeroExit, TimedOut, or Canceled.</summary>
        public ProcessFailureKind Kind { get; }
        /// <summary>Gets the executable path.</summary>
        public string ExecutablePath { get; }
        /// <summary>Gets the process exit code, when available.</summary>
        public int? ExitCode { get; }
        /// <summary>Gets all captured standard output.</summary>
        public string StandardOutput { get; }
        /// <summary>Gets all captured standard error.</summary>
        public string StandardError { get; }
    }
    /// <summary>Describes a canceled CLI process execution and retains its captured process details.</summary>
    public sealed class ProcessExecutionCanceledException : OperationCanceledException {
        internal ProcessExecutionCanceledException(ProcessExecutionException failureDetails, CancellationToken cancellationToken)
            : base(failureDetails.Message, failureDetails, cancellationToken) {
            FailureDetails = failureDetails;
        }

        /// <summary>Gets the captured executable path, exit code, standard output, and standard error.</summary>
        public ProcessExecutionException FailureDetails { get; }
    }

    internal sealed class ProcessResult {
        internal ProcessResult(int exitCode, string standardOutput, string standardError) {
            ExitCode = exitCode;
            StandardOutput = standardOutput;
            StandardError = standardError;
        }
        internal int ExitCode { get; }
        internal string StandardOutput { get; }
        internal string StandardError { get; }
    }

    internal static class ProcessRunner {
        internal static readonly TimeSpan DefaultTimeout = TimeSpan.FromMinutes(2);

        internal static ProcessResult Run(string executablePath, IEnumerable<string> arguments, TimeSpan timeout, CancellationToken cancellationToken) {
            if (string.IsNullOrWhiteSpace(executablePath)) throw new ArgumentException("An executable path is required.", nameof(executablePath));
            if (arguments == null) throw new ArgumentNullException(nameof(arguments));
            if (timeout <= TimeSpan.Zero || timeout.TotalMilliseconds > int.MaxValue) throw new ArgumentOutOfRangeException(nameof(timeout));
            cancellationToken.ThrowIfCancellationRequested();

            var output = new StringBuilder();
            var error = new StringBuilder();
            var outputLock = new object();
            var errorLock = new object();
            using (var process = new Process()) {
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.FileName = executablePath;
                process.StartInfo.Arguments = JoinArguments(arguments);
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.OutputDataReceived += (sender, e) => { if (e.Data != null) lock (outputLock) output.AppendLine(e.Data); };
                process.ErrorDataReceived += (sender, e) => { if (e.Data != null) lock (errorLock) error.AppendLine(e.Data); };

                try {
                    if (!process.Start()) throw new InvalidOperationException("The process did not start.");
                } catch (Exception ex) {
                    throw Failure("Failed to start the WebP CLI process.", ProcessFailureKind.StartFailed, executablePath, null, output, outputLock, error, errorLock, ex);
                }

                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                using (cancellationToken.Register(() => TryKill(process))) {
                    var exited = process.WaitForExit((int)Math.Ceiling(timeout.TotalMilliseconds));
                    if (!exited) TryKill(process);
                    process.WaitForExit();
                    if (cancellationToken.IsCancellationRequested) {
                        var failure = Failure("WebP CLI process execution was canceled.", ProcessFailureKind.Canceled, executablePath, SafeExitCode(process), output, outputLock, error, errorLock);
                        throw new ProcessExecutionCanceledException(failure, cancellationToken);
                    }
                    if (!exited) {
                        throw Failure("WebP CLI process exceeded the execution timeout.", ProcessFailureKind.TimedOut, executablePath, SafeExitCode(process), output, outputLock, error, errorLock);
                    }
                }

                var result = new ProcessResult(process.ExitCode, GetText(output, outputLock), GetText(error, errorLock));
                if (result.ExitCode != 0) {
                    throw new ProcessExecutionException("WebP CLI process exited with a nonzero status.", ProcessFailureKind.NonZeroExit, executablePath, result.ExitCode, result.StandardOutput, result.StandardError);
                }
                return result;
            }
        }

        // The builder's existing public argument representation is a whitespace-delimited CLI string.
        // It contains only generated option/value tokens; paths are always supplied separately as arguments.
        internal static List<string> ParseArguments(string arguments) {
            return string.IsNullOrWhiteSpace(arguments)
                ? new List<string>()
                : arguments.Split((char[])null, StringSplitOptions.RemoveEmptyEntries).ToList();
        }

        private static string JoinArguments(IEnumerable<string> arguments) {
            return string.Join(" ", arguments.Select(QuoteArgument));
        }

        private static string QuoteArgument(string argument) {
            if (argument == null) throw new ArgumentException("Process arguments cannot contain null values.", nameof(argument));
            var result = new StringBuilder("\"");
            var backslashes = 0;
            foreach (var character in argument) {
                if (character == '\\') { backslashes++; continue; }
                if (character == '"') {
                    result.Append('\\', backslashes * 2 + 1).Append('"');
                    backslashes = 0;
                    continue;
                }
                result.Append('\\', backslashes).Append(character);
                backslashes = 0;
            }
            result.Append('\\', backslashes * 2).Append('"');
            return result.ToString();
        }

        private static void TryKill(Process process) {
            try { if (!process.HasExited) process.Kill(); } catch (InvalidOperationException) { } catch (System.ComponentModel.Win32Exception) { }
        }

        private static int? SafeExitCode(Process process) {
            try { return process.ExitCode; } catch (InvalidOperationException) { return null; }
        }

        private static ProcessExecutionException Failure(string message, ProcessFailureKind kind, string path, int? exitCode, StringBuilder output, object outputLock, StringBuilder error, object errorLock, Exception inner = null) {
            return new ProcessExecutionException(message, kind, path, exitCode, GetText(output, outputLock), GetText(error, errorLock), inner);
        }

        private static string GetText(StringBuilder builder, object sync) {
            lock (sync) return builder.ToString();
        }
    }
}
