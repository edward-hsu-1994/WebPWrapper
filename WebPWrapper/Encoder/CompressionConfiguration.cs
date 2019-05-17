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
        public void Lossless(Expression<Action<LosslessCompressionConfiguration>> config) {
            var _losslessCompressionConfiguration = new LosslessCompressionConfiguration();
            config.Compile().Invoke(_losslessCompressionConfiguration);

            _arguments.Add((
                key: nameof(Lossless),
                value: _losslessCompressionConfiguration.GetCurrentArguments()
            ));
        }

        /// <summary>
        /// 無損壓縮
        /// </summary>
        /// <param name="config">壓縮設定</param>
        public void NearLossless(Expression<Action<NearLosslessCompressionConfiguration>> config) {
            var _nearLosslessCompressionConfiguration = new NearLosslessCompressionConfiguration();
            config.Compile().Invoke(_nearLosslessCompressionConfiguration);

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