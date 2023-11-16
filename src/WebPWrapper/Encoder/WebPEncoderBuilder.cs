using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.InteropServices;
using System.Text;

namespace WebPWrapper.Encoder {
    /// <summary>
    /// Default WebP encoder builder
    /// </summary>
    public class WebPEncoderBuilder : IWebPEncoderBuilder {
        // temp cli arguments
        private List<(string key, string value)> _arguments = new List<(string key, string value)>();

        private string _executeFilePath;


        public const string _windowsDir = "libwebp-1.3.1-windows-x64";
        public const string _linuxDir = "libwebp-1.3.1-linux-x86-64";
        public const string _osxDir = "libwebp-1.3.1-mac-x86-64";
        public const string _osxARMDir = "libwebp-1.3.1-mac-arm64";
        /// <summary>
        /// Create <see cref="WebPEncoderBuilder"/>
        /// </summary>
        /// <param name="executeFilePath">Cli file path.</param>
        /// <exception cref="PlatformNotSupportedException"></exception>
        /// <exception cref="FileNotFoundException"></exception>
        public WebPEncoderBuilder(string executeFilePath = null) {
            _executeFilePath = executeFilePath;
            if (_executeFilePath == null) {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                    _executeFilePath = $"webp/{_windowsDir}/bin/cwebp.exe";
                } else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
                    _executeFilePath = $"webp/{_linuxDir}/bin/cwebp";
                }
                else if (RuntimeInformation.ProcessArchitecture == Architecture.Arm64 && RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    _executeFilePath = $"webp/{_osxARMDir}/bin/cwebp";
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
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

        public IWebPEncoderBuilder Crop(int x, int y, int width, int height) {
            _arguments.Add((key: "-crop", value: $"{x} {y} {width} {height}"));
            return this;
        }

        public IWebPEncoderBuilder Resize(int width, int height) {
            _arguments.Add((key: "-resize", $"{width} {height}"));
            return this;
        }

        public IWebPEncoderBuilder MultiThread() {
            _arguments.Add((key: "-mt", value: null));
            return this;
        }

        public IWebPEncoderBuilder LowMemory() {
            _arguments.Add((key: "-low_memory", value: null));
            return this;
        }

        public IWebPEncoderBuilder CopyMetadata(params Metadatas[] metadatas) {
            if (metadatas == null || metadatas.Length == 0) {
                throw new ArgumentNullException(nameof(metadatas));
            }
            _arguments.Add((key: "-metadata", value: string.Join(",", metadatas.Select(x => x.ToString().ToLower()))));
            return this;
        }

        public IWebPEncoderBuilder DisableAssemblyOptimization() {
            _arguments.Add((key: "-noasm", value: null));
            return this;
        }

        public IWebPEncoderBuilder LoadPresetProfile(PresetProfiles profile) {
            _arguments.Add((key: "-preset", value: profile.ToString().ToLower()));
            return this;
        }

        public IWebPEncoderBuilder CompressionConfig(Expression<Action<CompressionConfiguration>> config) {
            var _compressionConfiguration = new CompressionConfiguration();
            config.Compile().Invoke(_compressionConfiguration);

            _arguments.Add((key: nameof(CompressionConfig), value: _compressionConfiguration.GetCurrentArguments()));
            return this;
        }

        public IWebPEncoderBuilder AlphaConfig(Expression<Action<AlphaConfiguration>> config) {
            var _alphaConfiguration = new AlphaConfiguration();
            config.Compile().Invoke(_alphaConfiguration);

            _arguments.Add((key: nameof(AlphaConfig), value: _alphaConfiguration.GetCurrentArguments()));
            return this;
        }

        public IWebPEncoderBuilder Reset() {
            _arguments.Clear();
            return this;
        }

        public IWebPEncoder Build() {
            var args = GetCurrentArguments();
            return new WebPEncoder(_executeFilePath, args);
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
