using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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
        /// <summary>Directory name for the bundled Windows decoder.</summary>
        public const string _windowsDir = "libwebp-1.6.0-windows-x64";
        /// <summary>Directory name for the bundled Linux decoder.</summary>
        public const string _linuxDir = "libwebp-1.6.0-linux-x86-64";
        /// <summary>Directory name for the bundled macOS x64 decoder.</summary>
        public const string _osxDir = "libwebp-1.6.0-mac-x86-64";
        /// <summary>Directory name for the bundled macOS ARM64 decoder.</summary>
        public const string _osxARMDir = "libwebp-1.6.0-mac-arm64";

        /// <summary>
        /// Create <see cref="WebPDecoderBuilder"/>
        /// </summary>
        /// <param name="executeFilePath">Cli file path.</param>
        /// <exception cref="PlatformNotSupportedException">The current platform is unsupported.</exception>
        /// <exception cref="FileNotFoundException">The decoder executable does not exist.</exception>
        public WebPDecoderBuilder(string executeFilePath = null) {
            _executeFilePath = executeFilePath ?? WebPExecuteDownloader.GetDefaultExecutablePath(encoder: false);
            if (!File.Exists(_executeFilePath)) {
                throw new FileNotFoundException();
            }
        }

        /// <summary>Selects the decoded image output format.</summary>
        /// <param name="format">The output format.</param>
        /// <returns>This builder, for chaining additional decoding options.</returns>
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

        /// <summary>Crops the decoded image to the specified rectangle.</summary>
        /// <param name="x">Left coordinate.</param><param name="y">Top coordinate.</param><param name="width">Crop width.</param><param name="height">Crop height.</param>
        /// <returns>This builder, for chaining additional decoding options.</returns>
        public IWebPDecoderBuilder Crop(int x, int y, int width, int height) {
            _arguments.Add((key: "-crop", value: $"{x} {y} {width} {height}"));
            return this;
        }

        /// <summary>Resizes the decoded image.</summary>
        /// <param name="width">Output width.</param><param name="height">Output height.</param>
        /// <returns>This builder, for chaining additional decoding options.</returns>
        public IWebPDecoderBuilder Resize(int width, int height) {
            _arguments.Add((key: "-resize", $"{width} {height}"));
            return this;
        }

        /// <summary>Enables multithreaded decoding when supported.</summary>
        /// <returns>This builder, for chaining additional decoding options.</returns>
        public IWebPDecoderBuilder MultiThread() {
            _arguments.Add((key: "-mt", value: null));
            return this;
        }

        /// <summary>Disables assembly optimizations.</summary>
        /// <returns>This builder, for chaining additional decoding options.</returns>
        public IWebPDecoderBuilder DisableAssemblyOptimization() {
            _arguments.Add((key: "-noasm", value: null));
            return this;
        }

        /// <summary>Disables in-loop filtering.</summary>
        /// <returns>This builder, for chaining additional decoding options.</returns>
        public IWebPDecoderBuilder NoFilter() {
            _arguments.Add((key: "-nofilter", value: null));
            return this;
        }

        /// <summary>Disables the fancy upsampling filter.</summary>
        /// <returns>This builder, for chaining additional decoding options.</returns>
        public IWebPDecoderBuilder NoFancy() {
            _arguments.Add((key: "-nofancy", value: null));
            return this;
        }

        /// <summary>Sets dithering strength for RGB-to-YUV conversion.</summary>
        /// <param name="strength">Dithering strength.</param>
        /// <returns>This builder, for chaining additional decoding options.</returns>
        public IWebPDecoderBuilder Dither(int strength) {
            _arguments.Add((key: "-dither", value: strength.ToString()));
            return this;
        }

        /// <summary>Flips the decoded image vertically.</summary>
        /// <returns>This builder, for chaining additional decoding options.</returns>
        public IWebPDecoderBuilder Flip() {
            _arguments.Add((key: "-flip", value: null));
            return this;
        }

        /// <summary>Clears all previously selected decoder options.</summary>
        /// <returns>This builder after clearing previously selected decoder options.</returns>
        public IWebPDecoderBuilder Reset() {
            _arguments.Clear();
            return this;
        }

        /// <summary>Creates a decoder configured with the current options.</summary>
        /// <returns>The configured decoder.</returns>
        public IWebPDecoder Build() {
            var args = GetCurrentArguments();
            return new WebPDecoder(_executeFilePath, args);
        }

        /// <summary>Returns the command-line arguments accumulated by this builder.</summary>
        /// <returns>The formatted decoder command-line arguments.</returns>
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
