using System.Collections.Generic;
using System.Linq;

namespace WebPWrapper.Encoder {
    public class NearLosslessCompressionConfiguration {
        /// <summary>
        /// 建構中的參數暫存
        /// </summary>
        private List<(string key, string value)> _arguments = new List<(string key, string value)>();

        internal NearLosslessCompressionConfiguration() { }

        /// <summary>
        /// 取得目前CLI參數
        /// </summary>
        /// <returns>CLI參數</returns>
        internal string GetCurrentArguments() {
            return string.Join(" ", _arguments.Select(x => {
                if (x.key.StartsWith("-")) {
                    return $"{x.key} {x.value}";
                } else {
                    return x.value;
                }
            }));
        }
    }
}