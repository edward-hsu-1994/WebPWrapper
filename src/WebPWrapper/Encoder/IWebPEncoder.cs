using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace WebPWrapper.Encoder {
    /// <summary>
    /// WebP encoder
    /// </summary>
    public interface IWebPEncoder {
        /// <summary>
        /// Encode.
        /// </summary>
        /// <param name="input">Input image</param>
        /// <param name="output">Output WebP image</param>
        Task EncodeAsync(Stream input, Stream output);

        /// <summary>
        /// Encode.
        /// </summary>
        /// <param name="input">Input image</param>
        /// <param name="output">Output WebP image</param>
        void Encode(Stream input, Stream output);
    }
}
