using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace WebPWrapper.Encoder {
    /// <summary>
    /// WebP編碼器
    /// </summary>
    public interface IWebPEncoder {
        /// <summary>
        /// 編碼
        /// </summary>
        /// <param name="input">輸入值</param>
        /// <param name="output">輸出值</param>
        Task EncodeAsync(Stream input, Stream output);

        /// <summary>
        /// 編碼
        /// </summary>
        /// <param name="input">輸入值</param>
        /// <param name="output">輸出值</param>
        void Encode(Stream input, Stream output);
    }
}
