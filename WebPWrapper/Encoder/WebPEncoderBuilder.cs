using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.InteropServices;
using System.Text;

namespace WebPWrapper.Encoder {
    public class WebPEncoderBuilder : IWebPEncoderBuilder {
        /// <summary>
        /// 建構中的參數暫存
        /// </summary>
        private Dictionary<string, string> _arguments = new Dictionary<string, string>();

        private string _executeFilePath;


        public const string _windowsDir = "libwebp-1.0.2-windows-x86";
        public const string _linuxDir = "libwebp-1.0.2-linux-x86-64";
        public const string _osxDir = "libwebp-1.0.2-mac-10.14";



        /// <summary>
        /// 初始化WebP編碼器建構器
        /// </summary>
        /// <param name="executeFilePath">執行檔路徑，如為空則使用預設路徑</param>
        public WebPEncoderBuilder(string executeFilePath = null) {
            _executeFilePath = executeFilePath;
            if (_executeFilePath == null) {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                    _executeFilePath = $"webp/{_windowsDir}/bin/cwebp.exe";
                } else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
                    _executeFilePath = $"webp/{_linuxDir}/bin/cwebp";
                } else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
                    _executeFilePath = $"webp/{_osxDir}/bin/cwebp";
                } else {
                    throw new PlatformNotSupportedException();
                }
                _executeFilePath = Path.Combine(Path.GetFullPath("."), _executeFilePath);
            }

            if (!File.Exists(_executeFilePath)) {
                throw new FileNotFoundException();
            }
        }

        /// <summary>
        /// 輸入圖片裁減
        /// </summary>
        /// <param name="x">起始座標X</param>
        /// <param name="y">起始座標Y</param>
        /// <param name="width">寬度</param>
        /// <param name="height">高度</param> 
        public WebPEncoderBuilder Crop(uint x, uint y, uint width, uint height) {
            if (_arguments.ContainsKey("-crop")) {
                _arguments.Remove("-crop");
            }
            _arguments["-crop"] = $"{x} {y} {width} {height}";
            return this;
        }

        /// <summary>
        /// 縮放圖片，<paramref name="height"/>與<paramref name="width"/>至少一者非0，如果其中一值為0則等比例縮放
        /// </summary>
        /// <param name="width">寬度</param>
        /// <param name="height">寬度</param> 
        public WebPEncoderBuilder Resize(uint width, uint height) {
            if (_arguments.ContainsKey("-resize")) {
                _arguments.Remove("-resize");
            }
            _arguments["-resize"] = $"{width} {height}";
            return this;
        }

        /// <summary>
        /// 容許多執行序
        /// </summary>
        public WebPEncoderBuilder MultiThread() {
            if (_arguments.ContainsKey("-mt")) {
                _arguments.Remove("-mt");
            }
            _arguments["-mt"] = null;
            return this;
        }

        /// <summary>
        /// 降低記憶體使用
        /// </summary>
        public WebPEncoderBuilder LowMemory() {
            if (_arguments.ContainsKey("-low_memory")) {
                _arguments.Remove("-low_memory");
            }
            _arguments["-low_memory"] = null;
            return this;
        }

        /// <summary>
        /// 複製來源圖片的Metadata
        /// </summary>
        public WebPEncoderBuilder CopyMetadata(params Metadatas[] metadatas) {
            if (metadatas == null || metadatas.Length == 0) {
                throw new ArgumentNullException(nameof(metadatas));
            }
            if (_arguments.ContainsKey("-metadata")) {
                _arguments.Remove("-metadata");
            }
            _arguments["-metadata"] = string.Join(",", metadatas.Select(x => x.ToString().ToLower()));
            return this;
        }

        /// <summary>
        /// 停用ASM優化
        /// </summary>
        public WebPEncoderBuilder DisableAssemblyOptimization() {
            if (_arguments.ContainsKey("-noasm")) {
                _arguments.Remove("-noasm");
            }
            _arguments["-noasm"] = null;
            return this;
        }

        /// <summary>
        /// 讀取預設的組態
        /// </summary>
        /// <param name="profile">組態類型</param> 
        public WebPEncoderBuilder LoadPresetProfile(PresetProfiles profile) {
            if (_arguments.ContainsKey("-preset")) {
                _arguments.Remove("-preset");
            }
            _arguments["-preset"] = profile.ToString().ToLower();
            return this;
        }

        /// <summary>
        /// 設定壓縮組態
        /// </summary>
        /// <param name="config">壓縮組態設定</param> 
        public WebPEncoderBuilder CompressionConfig(Expression<Action<CompressionConfiguration>> config) {
            var _compressionConfiguration = new CompressionConfiguration();
            config.Compile().Invoke(_compressionConfiguration);

            _arguments[nameof(CompressionConfig)] = "";
            return this;
        }

        /// <summary>
        /// 設定Alpha組態
        /// </summary>
        /// <param name="config">Alpha組態設定</param>
        public WebPEncoderBuilder AlphaConfig(Expression<Action<AlphaConfiguration>> config) {
            var _alphaConfiguration = new AlphaConfiguration();
            config.Compile().Invoke(_alphaConfiguration);

            _arguments[nameof(AlphaConfig)] = "";
            return this;
        }

        /// <summary>
        /// 重設回預設值
        /// </summary>
        public WebPEncoderBuilder Reset() {
            _arguments.Clear();
            return this;
        }

        /// <summary>
        /// 建構WebP編碼器
        /// </summary>
        /// <returns>WebP編碼器</returns>
        public IWebPEncoder Build() {
            var args = GetCurrentArguments();
            return new WebPEncoder(_executeFilePath, args);
        }

        /// <summary>
        /// 取得目前CLI參數
        /// </summary>
        /// <returns>CLI參數</returns>
        public string GetCurrentArguments() {
            return string.Join(" ", _arguments.Select(x => {
                if (x.Key.StartsWith("-")) {
                    return $"{x.Key} {x.Value}";
                } else {
                    return x.Value;
                }
            }));
        }
    }
}
