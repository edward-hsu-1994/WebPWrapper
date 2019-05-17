using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WebPWrapper.Encoder {
    public class LosslessConfiguration {
        /// <summary>
        /// 建構中的參數暫存
        /// </summary>
        private List<(string key, string value)> _arguments = new List<(string key, string value)>();

        internal LosslessConfiguration() { }

        /// <summary>
        /// 設定無損壓縮模式
        /// </summary>
        /// <param name="mode">範圍自0~9，0最快，9最慢，最佳預設為6</param> 
        public LosslessConfiguration Mode(int mode) {
            if (mode < 0 || mode > 9) {
                throw new ArgumentOutOfRangeException(nameof(mode));
            }

            _arguments.Add(("-z", mode.ToString()));
            return this;
        }

        /// <summary>
        /// 設定壓縮品質
        /// </summary>
        /// <param name="quality">範圍自0~100，0最快，100最慢，預設為75，較小的數值將產生較大的文件</param>
        public LosslessConfiguration Quality(int quality) {
            if (quality < 0 || quality > 100) {
                throw new ArgumentOutOfRangeException(nameof(quality));
            }

            _arguments.Add(("-q", quality.ToString()));
            return this;
        }

        /// <summary>
        /// 指定壓縮方法，此參數控制編碼速度以及壓縮文件大小與品質之間的關係
        /// </summary>
        /// <param name="method">範圍自0~6，0最快，6最慢，預設為4，較小的數值將產生更大的文件</param>
        public LosslessConfiguration Method(int method) {
            if (method < 0 || method > 6) {
                throw new ArgumentOutOfRangeException(nameof(method));
            }

            _arguments.Add(("-m", method.ToString()));
            return this;
        }

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
