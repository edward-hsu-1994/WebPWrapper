using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace WebPWrapper.Encoder {
    /// <summary>
    /// WebP encoder
    /// </summary>
    public interface IWebPEncoder {
        /// <summary>Encodes an image asynchronously.</summary>
        /// <param name="input">Input image.</param>
        /// <param name="output">Output WebP image.</param>
        /// <returns>A task representing the encoding operation.</returns>
        Task EncodeAsync(Stream input, Stream output);
        /// <summary>Encodes an image asynchronously and terminates the CLI process if canceled.</summary>
        /// <param name="input">Input image.</param>
        /// <param name="output">Output WebP image.</param>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        /// <returns>A task representing the encoding operation.</returns>
        Task EncodeAsync(Stream input, Stream output, CancellationToken cancellationToken);

        /// <summary>
        /// Encode.
        /// </summary>
        /// <param name="input">Input image</param>
        /// <param name="output">Output WebP image</param>
        void Encode(Stream input, Stream output);
    }
}
