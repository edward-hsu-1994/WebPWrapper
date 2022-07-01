using System.Collections.Generic;
using System.Linq;

namespace WebPWrapper.Encoder {
    /// <summary>
    /// NearLosslessFilter configuration
    /// </summary>
    public class NearLosslessFilterConfiguration {
        // temp cli arguments
        private List<(string key, string value)> _arguments = new List<(string key, string value)>();

        internal NearLosslessFilterConfiguration() { }

        /// <summary>
        /// Turns auto-filter on. This algorithm will spend additional time optimizing the filtering strength to reach a well-balanced quality.
        /// </summary>
        public void Auto() {
            _arguments.Add((key: "-af", value: null));
        }

        /// <summary>
        /// Specify the strength of the deblocking filter.
        /// </summary>
        /// <param name="strength">Strength between 0 (no filtering) and 100 (maximum filtering). A value of 0 will turn off any filtering. Higher value will increase the strength of the filtering process applied after decoding the picture. The higher the value the smoother the picture will appear. Typical values are usually in the range of 20 to 50.</param>
        public NearLosslessFilterConfiguration Strength(int strength) {
            _arguments.Add((key: "-f", value: strength.ToString()));
            return this;
        }

        /// <summary>
        /// Specify the sharpness of the filtering (if used).
        /// </summary>
        /// <param name="sharpness">Sharpness range is 0 (sharpest) to 7 (least sharp). Default is 0.</param> 
        public NearLosslessFilterConfiguration Sharpness(int sharpness) {
            _arguments.Add((key: "-sharpness", value: sharpness.ToString()));
            return this;
        }

        /// <summary>
        /// Disable strong filtering (if filtering is being used thanks to the -f option) and use simple filtering instead.
        /// </summary>
        public NearLosslessFilterConfiguration NoStrong() {
            _arguments.Add((key: "-nostrong", value: null));
            return this;
        }

        /// <summary>
        /// Use more accurate and sharper RGB->YUV conversion if needed. Note that this process is slower than the default 'fast' RGB->YUV conversion.
        /// </summary>
        public NearLosslessFilterConfiguration SharpYUV() {
            _arguments.Add((key: "-sharp_yuv", value: null));
            return this;
        }

        /// <summary>
        /// Specify the amplitude of the spatial noise shaping. Spatial noise shaping (or sns for short) refers to a general collection of built-in algorithms used to decide which area of the picture should use relatively less bits, and where else to better transfer these bits.
        /// </summary>
        /// <param name="sns">SNS range goes from 0 (algorithm is off) to 100 (the maximal effect). The default value is 50.</param> 
        public NearLosslessFilterConfiguration SNS(int sns) {
            _arguments.Add((key: "-sns", value: sns.ToString()));
            return this;
        }

        /// <summary>
        /// Specify the amplitude of the spatial noise shaping. Spatial noise shaping (or sns for short) refers to a general collection of built-in algorithms used to decide which area of the picture should use relatively less bits, and where else to better transfer these bits.
        /// </summary>
        /// <param name="sns">SNS range goes from 0 (algorithm is off) to 100 (the maximal effect). The default value is 50.</param> 
        /// <param name="segments">Change the number of partitions to use during the segmentation of the sns algorithm. Segments should be in range 1 to 4. Default value is 4. This option has no effect for methods 3 and up, unless -low_memory is used.</param>
        public NearLosslessFilterConfiguration SNS(int sns, int segments) {
            _arguments.Add((key: "-sns", value: sns.ToString()));
            _arguments.Add((key: "-segments", value: segments.ToString()));
            return this;
        }

        /// <summary>
        /// Degrade quality by limiting the number of bits used by some macroblocks.
        /// </summary>
        /// <param name="limit">Range is 0 (no degradation, the default) to 100 (full degradation). </param>
        public NearLosslessFilterConfiguration PartitionLimit(int limit) {
            _arguments.Add((key: "-partition_limit", value: limit.ToString()));
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