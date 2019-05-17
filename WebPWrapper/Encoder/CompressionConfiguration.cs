using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace WebPWrapper.Encoder {
    public class CompressionConfiguration {
        /// <summary>
        /// 建構中的參數暫存
        /// </summary>
        private List<(string key, string value)> _arguments = new List<(string key, string value)>();

        internal CompressionConfiguration() { }

        /// <summary>
        /// 無損壓縮
        /// </summary>
        /// <param name="config">壓縮設定</param>
        public void Lossless(Expression<Action<LosslessConfiguration>> config) {
            var _losslessCompressionConfiguration = new LosslessConfiguration();
            config.Compile().Invoke(_losslessCompressionConfiguration);

            _arguments.Add((key: "-lossless", value: null));
            _arguments.Add((
                key: nameof(Lossless),
                value: _losslessCompressionConfiguration.GetCurrentArguments()
            ));
        }

        /// <summary>
        /// 接近無損壓縮
        /// </summary>
        /// <param name="config">壓縮設定</param>
        /// <param name="level">等級，最小0，最大100效果等同-lossless</param>
        public void NearLossless(int level, Expression<Action<NearLosslessConfiguration>> config) {
            var _nearLosslessCompressionConfiguration = new NearLosslessConfiguration();
            config.Compile().Invoke(_nearLosslessCompressionConfiguration);

            _arguments.Add((key: "-near_lossless", value: level.ToString()));
            _arguments.Add((
                key: nameof(NearLossless),
                value: _nearLosslessCompressionConfiguration.GetCurrentArguments()
            ));
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