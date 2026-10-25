using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace WebPWrapper.Decoder {
    /// <summary>Default WebP decoder.</summary>
    public class WebPDecoder : IWebPDecoder {
        private readonly string _executeFilePath;
        private readonly string _arguments;

        internal WebPDecoder(string executeFilePath, string arguments) {
            _executeFilePath = executeFilePath;
            _arguments = arguments;
        }
        
        /// <summary>Decodes WebP data from <paramref name="input"/> into <paramref name="output"/>.</summary>
        /// <param name="input">Readable stream containing the WebP image.</param>
        /// <param name="output">Writable stream receiving the decoded image.</param>
        public void Decode(Stream input, Stream output) {
            Decode(input, output, CancellationToken.None);
        }

        private void Decode(Stream input, Stream output, CancellationToken cancellationToken) {
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


        /// <summary>Asynchronously decodes WebP data from <paramref name="input"/> into <paramref name="output"/>.</summary>
        /// <param name="input">Readable stream containing the WebP image.</param>
        /// <param name="output">Writable stream receiving the decoded image.</param>
        /// <returns>A task that completes when decoding has finished.</returns>
        public Task DecodeAsync(Stream input, Stream output) => DecodeAsync(input, output, CancellationToken.None);

        /// <summary>Asynchronously decodes WebP data and observes cancellation.</summary>
        /// <param name="input">Readable stream containing the WebP image.</param>
        /// <param name="output">Writable stream receiving the decoded image.</param>
        /// <param name="cancellationToken">Token used to cancel the operation and terminate the CLI process.</param>
        /// <returns>A task representing the decoding operation.</returns>
        public Task DecodeAsync(Stream input, Stream output, CancellationToken cancellationToken) {
            return Task.Run(() => Decode(input, output, cancellationToken), cancellationToken);
        }
    }
}
