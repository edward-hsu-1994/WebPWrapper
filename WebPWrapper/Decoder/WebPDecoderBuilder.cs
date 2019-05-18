using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace WebPWrapper.Decoder {
    public class WebPDecoderBuilder : IWebPDecoderBuilder {
        /// <summary>
        /// 建構中的參數暫存
        /// </summary>
        private List<(string key, string value)> _arguments = new List<(string key, string value)>();

        private string _executeFilePath;


        public const string _windowsDir = "libwebp-1.0.2-windows-x86";
        public const string _linuxDir = "libwebp-1.0.2-linux-x86-64";
        public const string _osxDir = "libwebp-1.0.2-mac-10.14";

        /// <summary>
        /// 初始化WebP解碼器建構器
        /// </summary>
        /// <param name="executeFilePath">執行檔路徑，如為空則使用預設路徑</param>
        public WebPDecoderBuilder(string executeFilePath = null) {
            _executeFilePath = executeFilePath;
            if (_executeFilePath == null) {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                    _executeFilePath = $"webp/{_windowsDir}/bin/dwebp.exe";
                } else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
                    _executeFilePath = $"webp/{_linuxDir}/bin/dwebp";
                } else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
                    _executeFilePath = $"webp/{_osxDir}/bin/dwebp";
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
        /// 輸出格式
        /// </summary>
        /// <param name="format">格式</param> 
        public IWebPDecoderBuilder ExportFormat(ExportFormats format) {
            switch (format) {
                case ExportFormats.BMP:
                    _arguments.Add((key: "-bmp", value: null));
                    break;
                case ExportFormats.TIFF:
                    _arguments.Add((key: "-tiff", value: null));
                    break;
                case ExportFormats.PAM:
                    _arguments.Add((key: "-pam", value: null));
                    break;
                case ExportFormats.PPM:
                    _arguments.Add((key: "-ppm", value: null));
                    break;
                case ExportFormats.PGM:
                    _arguments.Add((key: "-pgm", value: null));
                    break;
                case ExportFormats.YUV:
                    _arguments.Add((key: "-yuv", value: null));
                    break;
            }
            return this;
        }

        /// <summary>
        /// 輸入圖片裁減
        /// </summary>
        /// <param name="x">起始座標X</param>
        /// <param name="y">起始座標Y</param>
        /// <param name="width">寬度</param>
        /// <param name="height">高度</param> 
        public IWebPDecoderBuilder Crop(int x, int y, int width, int height) {
            _arguments.Add((key: "-crop", value: $"{x} {y} {width} {height}"));
            return this;
        }

        /// <summary>
        /// 縮放圖片，<paramref name="height"/>與<paramref name="width"/>至少一者非0，如果其中一值為0則等比例縮放
        /// </summary>
        /// <param name="width">寬度</param>
        /// <param name="height">寬度</param>   
        public IWebPDecoderBuilder Resize(int width, int height) {
            _arguments.Add((key: "-resize", $"{width} {height}"));
            return this;
        }

        /// <summary>
        /// 容許多執行序
        /// </summary>
        public IWebPDecoderBuilder MultiThread() {
            _arguments.Add((key: "-mt", value: null));
            return this;
        }

        /// <summary>
        /// 停用ASM優化
        /// </summary>
        public IWebPDecoderBuilder DisableAssemblyOptimization() {
            _arguments.Add((key: "-noasm", value: null));
            return this;
        }

        /// <summary>
        /// 禁止濾波過程
        /// </summary> 
        public IWebPDecoderBuilder NoFilter() {
            _arguments.Add((key: "-nofilter", value: null));
            return this;
        }

        /// <summary>
        /// 禁止YUV420升級器
        /// </summary>
        public IWebPDecoderBuilder NoFancy() {
            _arguments.Add((key: "-nofancy", value: null));
            return this;
        }

        /// <summary>
        /// 抖動強度，抖動是應用於有損壓縮中的色度分量的後處理效果。它有助於平滑漸變並避免條帶偽影
        /// </summary>
        /// <param name="strength">強度，最小0，最大100</param> 
        public IWebPDecoderBuilder Dither(int strength) {
            _arguments.Add((key: "-dither", value: strength.ToString()));
            return this;
        }

        /// <summary>
        /// 垂直翻轉解碼圖片
        /// </summary>
        public IWebPDecoderBuilder Flip() {
            _arguments.Add((key: "-flip", value: null));
            return this;
        }

        /// <summary>
        /// 重設回預設值
        /// </summary>
        public IWebPDecoderBuilder Reset() {
            _arguments.Clear();
            return this;
        }

        /// <summary>
        /// 建構WebP解碼器
        /// </summary>
        /// <returns>WebP解碼器</returns>
        public IWebPDecoder Build() {
            var args = GetCurrentArguments();
            return new WebPDecoder(_executeFilePath, args);
        }

        /// <summary>
        /// 取得目前CLI參數
        /// </summary>
        /// <returns>CLI參數</returns>
        public string GetCurrentArguments() {
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
