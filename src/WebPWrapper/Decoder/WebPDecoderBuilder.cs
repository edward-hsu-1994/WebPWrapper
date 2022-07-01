using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace WebPWrapper.Decoder {
    /// <summary>
    /// Default WebP decoder builder
    /// </summary>
    public class WebPDecoderBuilder : IWebPDecoderBuilder {
        // temp CLI arguments.
        private List<(string key, string value)> _arguments = new List<(string key, string value)>();

        // CLI file path.
        private string _executeFilePath;

        // Default CLI path. (If Cli downloaded by WebPExecuteDownloader.
        public const string _windowsDir = "libwebp-1.2.2-windows-x64";
        public const string _linuxDir = "libwebp-1.2.2-linux-x86-64";
        public const string _osxDir = "libwebp-1.2.2-mac-x86-64";

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
        
        public IWebPDecoderBuilder Crop(int x, int y, int width, int height) {
            _arguments.Add((key: "-crop", value: $"{x} {y} {width} {height}"));
            return this;
        }
        
        public IWebPDecoderBuilder Resize(int width, int height) {
            _arguments.Add((key: "-resize", $"{width} {height}"));
            return this;
        }
        
        public IWebPDecoderBuilder MultiThread() {
            _arguments.Add((key: "-mt", value: null));
            return this;
        }
        
        public IWebPDecoderBuilder DisableAssemblyOptimization() {
            _arguments.Add((key: "-noasm", value: null));
            return this;
        }
        
        public IWebPDecoderBuilder NoFilter() {
            _arguments.Add((key: "-nofilter", value: null));
            return this;
        }
        
        public IWebPDecoderBuilder NoFancy() {
            _arguments.Add((key: "-nofancy", value: null));
            return this;
        }
        
        public IWebPDecoderBuilder Dither(int strength) {
            _arguments.Add((key: "-dither", value: strength.ToString()));
            return this;
        }
        
        public IWebPDecoderBuilder Flip() {
            _arguments.Add((key: "-flip", value: null));
            return this;
        }
        
        public IWebPDecoderBuilder Reset() {
            _arguments.Clear();
            return this;
        }
        
        public IWebPDecoder Build() {
            var args = GetCurrentArguments();
            return new WebPDecoder(_executeFilePath, args);
        }
        
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
