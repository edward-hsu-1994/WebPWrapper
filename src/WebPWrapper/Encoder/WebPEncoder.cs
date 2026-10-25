using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace WebPWrapper.Encoder {
    /// <summary>Default WebP encoder.</summary>
    public class WebPEncoder : IWebPEncoder {
        private readonly string _executeFilePath;
        private readonly string _arguments;

        internal WebPEncoder(string executeFilePath, string arguments) {
            _executeFilePath = executeFilePath;
            _arguments = arguments;
        }

        /// <summary>Encodes image data from <paramref name="input"/> into <paramref name="output"/>.</summary>
        /// <param name="input">Readable stream containing the source image.</param>
        /// <param name="output">Writable stream receiving the WebP image.</param>
        public void Encode(Stream input, Stream output) {
            Encode(input, output, CancellationToken.None);
        }

        private void Encode(Stream input, Stream output, CancellationToken cancellationToken) {
            string inputFile = null;
            string outputFile = null;
            try {
                cancellationToken.ThrowIfCancellationRequested();
                inputFile = Path.GetTempFileName();
                outputFile = Path.GetTempFileName();
                using (var inputStream = File.Open(inputFile, FileMode.Open)) input.CopyTo(inputStream);
                cancellationToken.ThrowIfCancellationRequested();

                var arguments = ProcessRunner.ParseArguments(_arguments);
                arguments.Add("-o");
                arguments.Add(outputFile);
                arguments.Add(inputFile);
                ProcessRunner.Run(_executeFilePath, arguments, ProcessRunner.DefaultTimeout, cancellationToken);

                cancellationToken.ThrowIfCancellationRequested();
                using (var outputStream = File.Open(outputFile, FileMode.Open)) outputStream.CopyTo(output);
            } finally {
                if (inputFile != null) File.Delete(inputFile);
                if (outputFile != null) File.Delete(outputFile);
            }
        }

        /// <summary>Encodes an image on a worker thread.</summary>
        /// <param name="input">Input image.</param>
        /// <param name="output">Output WebP image.</param>
        /// <returns>A task representing the encoding operation.</returns>
        public Task EncodeAsync(Stream input, Stream output) => EncodeAsync(input, output, CancellationToken.None);

        /// <summary>Asynchronously encodes image data from <paramref name="input"/> into <paramref name="output"/>.</summary>
        /// <param name="input">Readable stream containing the source image.</param>
        /// <param name="output">Writable stream receiving the WebP image.</param>
        /// <param name="cancellationToken">Token used to cancel the operation and terminate the CLI process.</param>
        /// <returns>A task representing the encoding operation.</returns>
        public Task EncodeAsync(Stream input, Stream output, CancellationToken cancellationToken) {
            return Task.Run(() => Encode(input, output, cancellationToken), cancellationToken);
        }
    }
}
