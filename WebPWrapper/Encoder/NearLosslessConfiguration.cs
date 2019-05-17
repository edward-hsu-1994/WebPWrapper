using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace WebPWrapper.Encoder {
    public class NearLosslessConfiguration {
        /// <summary>
        /// 建構中的參數暫存
        /// </summary>
        private List<(string key, string value)> _arguments = new List<(string key, string value)>();

        internal NearLosslessConfiguration() { }

        /// <summary>
        /// 嘗試壓縮至指定大小
        /// </summary>
        /// <param name="size">大小(位元組)</param>
        /// <returns></returns>
        public void Size(int size) {
            _arguments.Add((key: "-size", value: size.ToString()));
        }

        /// <summary>
        /// 嘗試壓縮至指定大小
        /// </summary>
        /// <param name="size">大小(位元組)</param>
        /// <param name="pass">最大處理次數，範圍1~10，預設1</param>
        /// <returns></returns>
        public void Size(int size, int pass) {
            _arguments.Add((key: "-size", value: size.ToString()));
            _arguments.Add((key: "-pass", value: pass.ToString()));
        }

        /// <summary>
        /// 嘗試壓縮至指定PSNR
        /// </summary>
        /// <param name="dB">PSNR值</param>
        public void PSNR(int dB) {
            _arguments.Add((key: "-psnr", value: dB.ToString()));
        }

        /// <summary>
        /// 嘗試壓縮至指定PSNR
        /// </summary>
        /// <param name="dB">PSNR值</param>
        /// <param name="pass">最大處理次數，範圍1~10，預設1</param>
        public void PSNR(int dB, int pass = 1) {
            _arguments.Add((key: "-psnr", value: dB.ToString()));
            _arguments.Add((key: "-pass", value: pass.ToString()));
        }

        /// <summary>
        /// 設定壓縮品質
        /// </summary>
        /// <param name="quality">範圍自0~100，0最低，100最高，預設為75，較小的數值將產生較差的品質</param>
        public NearLosslessConfiguration Quality(int quality) {
            if (quality < 0 || quality > 100) {
                throw new ArgumentOutOfRangeException(nameof(quality));
            }

            _arguments.Add((key: "-q", value: quality.ToString()));
            return this;
        }

        /// <summary>
        /// 設定透明壓縮品質
        /// </summary>
        /// <param name="quality">範圍自0~100，0最低，100最高</param>
        public NearLosslessConfiguration AlphaQuality(int quality) {
            if (quality < 0 || quality > 100) {
                throw new ArgumentOutOfRangeException(nameof(quality));
            }

            _arguments.Add((key: "-alpha_q ", value: quality.ToString()));
            return this;
        }

        /// <summary>
        /// 更改內部參數映射以更好地匹配JPEG壓縮的預期大小，但視覺失真較小。
        /// </summary>
        public NearLosslessConfiguration JPEGLike() {
            _arguments.Add((key: "-jpeg_like", value: null));
            return this;
        }

        /// <summary>
        /// 在RGBA-> YUVA轉換期間觸發與質量相關的偽隨機抖動
        /// </summary>
        public NearLosslessConfiguration PseudoRandomDithering() {
            _arguments.Add((key: "-pre", value: "2"));
            return this;
        }


        /// <summary>
        /// 指定壓縮方法，此參數控制編碼速度以及壓縮文件大小與品質之間的關係
        /// </summary>
        /// <param name="method">範圍自0~6，0最快，6最慢，預設為4，較小的數值將產生更大的文件</param>
        public NearLosslessConfiguration Method(int method) {
            if (method < 0 || method > 6) {
                throw new ArgumentOutOfRangeException(nameof(method));
            }

            _arguments.Add(("-m", method.ToString()));
            return this;
        }

        public NearLosslessConfiguration Filter(Expression<Action<NearLosslessFilterConfiguration>> config) {
            var _nearLosslessFilterConfiguration = new NearLosslessFilterConfiguration();
            config.Compile().Invoke(_nearLosslessFilterConfiguration);

            _arguments.Add((key: nameof(Filter), value: _nearLosslessFilterConfiguration.GetCurrentArguments()));
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