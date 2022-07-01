using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace WebPWrapper.Decoder {
    /// <summary>
    /// WebP decoder
    /// </summary>
    public interface IWebPDecoder {
        /// <summary>
        /// Decode.
        /// </summary>
        /// <param name="input">Input WebP image</param>
        /// <param name="output">Output image</param>
        Task DecodeAsync(Stream input, Stream output);

        /// <summary>
        /// Decode.
        /// </summary>
        /// <param name="input">Input WebP image</param>
        /// <param name="output">Output image</param>
        void Decode(Stream input, Stream output);
    }
}
