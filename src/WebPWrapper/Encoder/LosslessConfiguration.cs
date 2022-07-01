using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WebPWrapper.Encoder {
    /// <summary>
    /// Lossless configuration
    /// </summary>
    public class LosslessConfiguration {
        // temp cli arguments
        private List<(string key, string value)> _arguments = new List<(string key, string value)>();

        internal LosslessConfiguration() { }

        /// <summary>
        /// Setting lossless compression mode.
        /// </summary>
        /// <param name="mode">Compression mode with the specified level between 0 and 9, with level 0 being the fastest, 9 being the slowest. Fast mode produces larger file size than slower ones. A good default is 6.</param> 
        public LosslessConfiguration Mode(int mode) {
            if (mode < 0 || mode > 9) {
                throw new ArgumentOutOfRangeException(nameof(mode));
            }

            _arguments.Add(("-z", mode.ToString()));
            return this;
        }

        /// <summary>
        /// Specify the compression factor for RGB channels between 0 and 100.
        /// </summary>
        /// <param name="quality">In case of lossy compression (default), a small factor produces a smaller file with lower quality. Best quality is achieved by using a value of 100. Default is 75.</param>
        public LosslessConfiguration Quality(int quality) {
            if (quality < 0 || quality > 100) {
                throw new ArgumentOutOfRangeException(nameof(quality));
            }

            _arguments.Add(("-q", quality.ToString()));
            return this;
        }

        /// <summary>
        /// Specify the compression method to use. This parameter controls the trade off between encoding speed and the compressed file size and quality.
        /// </summary>
        /// <param name="method">Possible values range from 0 to 6. Default value is 4. When higher values are used, the encoder will spend more time inspecting additional encoding possibilities and decide on the quality gain. Lower value can result in faster processing time at the expense of larger file size and lower compression quality.</param>
        public LosslessConfiguration Method(int method) {
            if (method < 0 || method > 6) {
                throw new ArgumentOutOfRangeException(nameof(method));
            }

            _arguments.Add(("-m", method.ToString()));
            return this;
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
