using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace WebPWrapper.Encoder {
    /// <summary>
    /// Alpha configuration
    /// </summary>
    public class AlphaConfiguration {
        // CLI temp arguments
        private List<(string key, string value)> _arguments = new List<(string key, string value)>();

        internal AlphaConfiguration() { }

        /// <summary>
        /// Specify the predictive filtering method for the alpha plane. One of none, fast or best, in increasing complexity and slowness order. Internally, alpha filtering is performed using four possible predictions (none, horizontal, vertical, gradient). The best mode will try each mode in turn and pick the one which gives the smaller size. The fast mode will just try to form an a priori guess without testing all modes.
        /// </summary>
        /// <param name="filter">Filtering method. Default is <see cref="AlphaFilters.Fast"/></param> 
        public AlphaConfiguration Filter(AlphaFilters filter) {
            _arguments.Add((key: "-alpha_filter", value: filter.ToString().ToLower()));
            return this;
        }

        /// <summary>
        /// No compression
        /// </summary>
        public AlphaConfiguration DisableCompression() {
            _arguments.Add((key: "-alpha_method", value: ""));
            return this;
        }

        /// <summary>
        /// Transparent process
        /// </summary>
        /// <param name="process">Process method</param>
        public AlphaConfiguration TransparentProcess(
            TransparentProcesses process) {
            switch (process) {
                case TransparentProcesses.Exact:
                    _arguments.Add((key: "-exact", value: null));
                    break;
                case TransparentProcesses.Remove:
                    _arguments.Add((key: "-noalpha", value: null));
                    break;
                case TransparentProcesses.Blend:
                    throw new ArgumentNullException("blendColor");
                    break;
            }
            return this;
        }

        /// <summary>
        /// Transparent process. (This method only works when <paramref name="process"/> = <see cref="TransparentProcesses.Blend"/>)
        /// </summary>
        /// <param name="process">Process method</param>
        /// <param name="blendColor">Blend color</param>
        public AlphaConfiguration TransparentProcess(
            TransparentProcesses process,
            Color blendColor) {
            switch (process) {
                case TransparentProcesses.Exact:
                    _arguments.Add((key: "-exact", value: null));
                    break;
                case TransparentProcesses.Remove:
                    _arguments.Add((key: "-noalpha", value: null));
                    break;
                case TransparentProcesses.Blend:
                    var color_R = String.Format("{0:X2}", blendColor.R);
                    var color_G = String.Format("{0:X2}", blendColor.G);
                    var color_B = String.Format("{0:X2}", blendColor.B);

                    _arguments.Add((key: "-blend_alpha", value: $"0x{color_R}{color_G}{color_B}"));
                    break;
            }
            return this;
        }


        /// <summary>
        /// Get current CLI arguments.
        /// </summary>
        /// <returns>Cli arguments</returns>
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