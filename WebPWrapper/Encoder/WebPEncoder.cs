using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace WebPWrapper.Encoder {
    /// <summary>
    /// WebP編碼器
    /// </summary>
    public class WebPEncoder : IWebPEncoder {
        private string _executeFilePath;
        private string _arguments;
        internal WebPEncoder(string executeFilePath, string arguments) {
            _executeFilePath = executeFilePath;
            _arguments = arguments;
        }

        public void Encode(Stream input, Stream output) {
            throw new NotImplementedException();
        }

        public async Task EncodeAsync(Stream input, Stream output) {
            await Task.Run(() => Encode(input, output));
        }
    }
}
