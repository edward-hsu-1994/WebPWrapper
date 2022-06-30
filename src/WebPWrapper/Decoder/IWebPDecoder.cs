using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace WebPWrapper.Decoder {
    /// <summary>
    /// WebP解碼器
    /// </summary>
    public interface IWebPDecoder {
        /// <summary>
        /// 解碼
        /// </summary>
        /// <param name="input">輸入值</param>
        /// <param name="output">輸出值</param>
        Task DecodeAsync(Stream input, Stream output);

        /// <summary>
        /// 解碼
        /// </summary>
        /// <param name="input">輸入值</param>
        /// <param name="output">輸出值</param>
        void Decode(Stream input, Stream output);
    }
}
