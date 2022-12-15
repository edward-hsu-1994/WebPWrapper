using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WebPWrapper.Encoder {
    /// <summary>
    /// Lossless configuration
    /// </summary>
    public class LossyConfiguration {
        // temp cli arguments
        private List<(string key, string value)> _arguments = new List<(string key, string value)>();

        internal LossyConfiguration() { }

        /// <summary>
        /// Setting lossless compression mode.
        /// </summary>
        /// <param name="mode">Compression mode with the specified level between 0 and 9, with level 0 being the fastest, 9 being the slowest. Fast mode produces larger file size than slower ones. A good default is 6.</param> 
        public LossyConfiguration Mode(int mode) {
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
        public LossyConfiguration Quality(int quality) {
            if (quality < 0 || quality > 100) {
                throw new ArgumentOutOfRangeException(nameof(quality));
            }

            _arguments.Add(("-q", quality.ToString()));
            return this;
        }

        /// <summary>
        /// Specify a target size (in bytes) to try and reach for the compressed output. The compressor will make several passes of partial encoding in order to get as close as possible to this target.
        /// </summary>
        /// <param name="size">Size(bytes)</param>
        public void Size(int size)
        {
            _arguments.Add((key: "-size", value: size.ToString()));
        }

        /// <summary>
        /// Specify a target size (in bytes) to try and reach for the compressed output. The compressor will make several passes of partial encoding in order to get as close as possible to this target.
        /// </summary>
        /// <param name="size">Size(bytes)</param>
        /// <param name="pass">Set a maximum number of passes to use during the dichotomy used by Size. Maximum value is 10, default is 6.</param>
        public void Size(int size, int pass)
        {
            _arguments.Add((key: "-size", value: size.ToString()));
            _arguments.Add((key: "-pass", value: pass.ToString()));
        }

        /// <summary>
        /// Specify the compression method to use. This parameter controls the trade off between encoding speed and the compressed file size and quality.
        /// </summary>
        /// <param name="method">Possible values range from 0 to 6. Default value is 4. When higher values are used, the encoder will spend more time inspecting additional encoding possibilities and decide on the quality gain. Lower value can result in faster processing time at the expense of larger file size and lower compression quality.</param>
        public LossyConfiguration Method(int method) {
            if (method < 0 || method > 6) {
                throw new ArgumentOutOfRangeException(nameof(method));
            }

            _arguments.Add(("-m", method.ToString()));
            return this;
        }

        /// <summary>
        /// Specify a target PSNR (in dB) to try and reach for the compressed output. The compressor will make several passes of partial encoding in order to get as close as possible to this target.
        /// </summary>
        /// <param name="dB">PSNR</param>
        public void PSNR(int dB)
        {
            _arguments.Add((key: "-psnr", value: dB.ToString()));
        }

        /// <summary>
        /// Specify a target PSNR (in dB) to try and reach for the compressed output. The compressor will make several passes of partial encoding in order to get as close as possible to this target.
        /// </summary>
        /// <param name="dB">PSNR</param>
        /// <param name="pass">Set a maximum number of passes to use during the dichotomy used by PSNR. Maximum value is 10, default is 6.</param>
        public void PSNR(int dB, int pass = 1)
        {
            _arguments.Add((key: "-psnr", value: dB.ToString()));
            _arguments.Add((key: "-pass", value: pass.ToString()));
        }


        /// <summary>
        /// Specify the compression factor for alpha compression.
        /// </summary>
        /// <param name="quality">Quality between 0 and 100. Lossless compression of alpha is achieved using a value of 100, while the lower values result in a lossy compression. The default is 100.</param>
        public LossyConfiguration AlphaQuality(int quality)
        {
            if (quality < 0 || quality > 100)
            {
                throw new ArgumentOutOfRangeException(nameof(quality));
            }

            _arguments.Add((key: "-alpha_q ", value: quality.ToString()));
            return this;
        }

        /// <summary>
        /// Change the internal parameter mapping to better match the expected size of JPEG compression.
        /// </summary>
        public LossyConfiguration JPEGLike()
        {
            _arguments.Add((key: "-jpeg_like", value: null));
            return this;
        }

        /// <summary>
        /// Specify some pre-processing steps.
        /// </summary>
        public LossyConfiguration PseudoRandomDithering(int pre = 2)
        {
            _arguments.Add((key: "-pre", value: pre.ToString()));
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
