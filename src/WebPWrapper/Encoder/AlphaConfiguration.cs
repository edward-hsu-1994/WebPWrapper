using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace WebPWrapper.Encoder {
    public class AlphaConfiguration {
        /// <summary>
        /// 建構中的參數暫存
        /// </summary>
        private List<(string key, string value)> _arguments = new List<(string key, string value)>();

        internal AlphaConfiguration() { }

        /// <summary>
        /// Alpha過濾方法
        /// </summary>
        /// <param name="filter">過濾方法</param> 
        public AlphaConfiguration Filter(AlphaFilters filter) {
            _arguments.Add((key: "-alpha_filter", value: filter.ToString().ToLower()));
            return this;
        }

        /// <summary>
        /// 禁止Alpha壓縮
        /// </summary>
        public AlphaConfiguration DisableCompression() {
            _arguments.Add((key: "-alpha_method", value: ""));
            return this;
        }

        /// <summary>
        /// 透明處理
        /// </summary>
        /// <param name="process">處理方法</param>
        /// <param name="blendColor">混合顏色，此選項在<paramref name="process"/>為<see cref="TransparentProcesses.Blend"/>的情況下才作用</param>
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
        /// 透明處理
        /// </summary>
        /// <param name="process">處理方法</param>
        /// <param name="blendColor">混合顏色，此選項在<paramref name="process"/>為<see cref="TransparentProcesses.Blend"/>的情況下才作用</param>
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