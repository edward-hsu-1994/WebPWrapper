using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace WebPWrapper.Decoder {
    /// <summary>
    /// WebP decoder
    /// </summary>
    public interface IWebPDecoder {
        /// <summary>Decodes an image asynchronously using a worker thread.</summary>
        /// <param name="input">Input WebP image.</param>
        /// <param name="output">Output image.</param>
        /// <returns>A task representing the decoding operation.</returns>
        Task DecodeAsync(Stream input, Stream output);
        /// <summary>Decodes an image asynchronously and terminates the CLI process if canceled.</summary>
        /// <param name="input">Input WebP image.</param>
        /// <param name="output">Output image.</param>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        /// <returns>A task representing the decoding operation.</returns>
        Task DecodeAsync(Stream input, Stream output, CancellationToken cancellationToken);

        /// <summary>
        /// Decode.
        /// </summary>
        /// <param name="input">Input WebP image</param>
        /// <param name="output">Output image</param>
        void Decode(Stream input, Stream output);
    }
}
