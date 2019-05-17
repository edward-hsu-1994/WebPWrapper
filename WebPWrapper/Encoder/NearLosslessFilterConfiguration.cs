using System.Collections.Generic;
using System.Linq;

namespace WebPWrapper.Encoder {
    public class NearLosslessFilterConfiguration {
        /// <summary>
        /// 建構中的參數暫存
        /// </summary>
        private List<(string key, string value)> _arguments = new List<(string key, string value)>();

        internal NearLosslessFilterConfiguration() { }

        /// <summary>
        /// 自動濾波
        /// </summary>
        public void Auto() {
            _arguments.Add((key: "-af", value: null));
        }

        /// <summary>
        /// 濾波強度，預設使用強過濾
        /// </summary>
        /// <param name="strength">強度，最小0，最大100，數值越高圖片越平滑，通常介於20~50</param>
        public NearLosslessFilterConfiguration Strength(int strength) {
            _arguments.Add((key: "-f", value: strength.ToString()));
            return this;
        }

        /// <summary>
        /// 過濾清晰度
        /// </summary>
        /// <param name="sharpness">清晰度，最小0，最大7，越小越銳利</param> 
        public NearLosslessFilterConfiguration Sharpness(int sharpness) {
            _arguments.Add((key: "-sharpness", value: sharpness.ToString()));
            return this;
        }

        /// <summary>
        /// 禁止強過濾
        /// </summary>
        /// <returns></returns>
        public NearLosslessFilterConfiguration NoStrong() {
            _arguments.Add((key: "-nostrong", value: null));
            return this;
        }

        /// <summary>
        /// 更準確和更清晰的RGB -> YUV轉換，此過程比默認的“快速”RGB-> YUV轉換慢
        /// </summary>
        public NearLosslessFilterConfiguration SharpYUV() {
            _arguments.Add((key: "-sharp_yuv", value: null));
            return this;
        }

        /// <summary>
        /// 指定空間噪聲整形的幅度
        /// </summary>
        /// <param name="sns">整形幅度，最小0，最大100，0為關閉，100最大效果，預設50</param> 
        public NearLosslessFilterConfiguration SNS(int sns) {
            _arguments.Add((key: "-sns", value: sns.ToString()));
            return this;
        }

        /// <summary>
        /// 指定空間噪聲整形的幅度
        /// </summary>
        /// <param name="sns">整形幅度，最小0，最大100，0為關閉，100最大效果，預設50</param>
        /// <param name="segments">SNS算法分區數，最小1，最大4，預設4</param>
        public NearLosslessFilterConfiguration SNS(int sns, int segments) {
            _arguments.Add((key: "-sns", value: sns.ToString()));
            _arguments.Add((key: "-segments", value: segments.ToString()));
            return this;
        }

        /// <summary>
        /// 通過限制某些宏塊使用的位數來降低質量
        /// </summary>
        /// <param name="limit">位數，最小0，最大100，0表示未降級，100表示完全降級</param>
        public NearLosslessFilterConfiguration PartitionLimit(int limit) {
            _arguments.Add((key: "-partition_limit", value: limit.ToString()));
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