using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace WebPWrapper.Encoder {
    /// <summary>
    /// Compression configuration
    /// </summary>
    public class CompressionConfiguration {
        // temp cli arguments 
        private List<(string key, string value)> _arguments = new List<(string key, string value)>();

        internal CompressionConfiguration() { }

        /// <summary>
        /// Use lossless compression.
        /// </summary>
        /// <param name="config">Config</param>
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
        /// Specify the level of near-lossless image preprocessing. This option adjusts pixel values to help compressibility, but has minimal impact on the visual quality. It triggers lossless compression mode automatically.
        /// </summary>
        /// <param name="config">Config</param>
        /// <param name="level">Level(0~100)</param>
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
        /// Get current CLI arguments.
        /// </summary>
        /// <returns>CLI arguments</returns>
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