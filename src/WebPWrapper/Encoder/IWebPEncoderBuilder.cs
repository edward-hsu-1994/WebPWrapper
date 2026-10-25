using System;
using System.Linq.Expressions;

namespace WebPWrapper.Encoder {
    /// <summary>Defines a fluent builder for configuring WebP encoding.</summary>
    public interface IWebPEncoderBuilder {
        /// <summary>
        /// Crop the decoded picture to a rectangle with top-left corner at coordinates (<paramref name="x"/>, <paramref name="y"/>) and size width x height. This cropping area must be fully contained within the source rectangle. The top-left corner will be snapped to even coordinates if needed. This option is meant to reduce the memory needed for cropping large images. Note: the cropping is applied before any scaling.
        /// </summary>
        /// <param name="x">Top-left corner X</param>
        /// <param name="y">Top-left corner Y</param>
        /// <param name="width">Width</param>
        /// <param name="height">Height</param>
        /// <returns>This builder, for chaining additional encoding options.</returns>
        IWebPEncoderBuilder Crop(int x, int y, int width, int height);

        /// <summary>
        /// Rescale the decoded picture. At least one of <paramref name="height"/> or <paramref name="width"/> is non-zero, and if one of the values is zero then the scaling is equal
        /// </summary>
        /// <param name="width">Width</param>
        /// <param name="height">Height</param>
        /// <returns>This builder, for chaining additional encoding options.</returns>
        IWebPEncoderBuilder Resize(int width, int height);

        /// <summary>
        /// Use multi-threading for decoding, if possible.
        /// </summary>
        /// <returns>This builder, for chaining additional encoding options.</returns>
        IWebPEncoderBuilder MultiThread();

        /// <summary>
        /// Reduce memory usage of lossy encoding by saving four times the compressed size (typically). This will make the encoding slower and the output slightly different in size and distortion.
        /// </summary>
        /// <returns>This builder, for chaining additional encoding options.</returns>
        IWebPEncoderBuilder LowMemory();

        /// <summary>
        /// A comma separated list of metadata to copy from the input to the output if present.
        /// </summary>
        /// <param name="metadatas">Metadata categories to preserve. At least one category must be specified.</param>
        /// <exception cref="ArgumentNullException"><paramref name="metadatas"/> is <see langword="null"/> or contains no categories.</exception>
        /// <returns>This builder, for chaining additional encoding options.</returns>
        IWebPEncoderBuilder CopyMetadata(params Metadatas[] metadatas);

        /// <summary>
        /// Disable all assembly optimizations.
        /// </summary>
        /// <returns>This builder, for chaining additional encoding options.</returns>
        IWebPEncoderBuilder DisableAssemblyOptimization();

        /// <summary>
        /// Specify a set of pre-defined parameters to suit a particular type of source material.
        /// </summary>
        /// <param name="profile">Profile</param> 
        /// <returns>This builder, for chaining additional encoding options.</returns>
        IWebPEncoderBuilder LoadPresetProfile(PresetProfiles profile);

        /// <summary>
        /// Compression configuration.
        /// </summary>
        /// <param name="config">Config</param> 
        /// <returns>This builder, for chaining additional encoding options.</returns>
        IWebPEncoderBuilder CompressionConfig(Expression<Action<CompressionConfiguration>> config);

        /// <summary>
        /// Alpha configuration.
        /// </summary>
        /// <param name="config">Config</param>
        /// <returns>This builder, for chaining additional encoding options.</returns>
        IWebPEncoderBuilder AlphaConfig(Expression<Action<AlphaConfiguration>> config);

        /// <summary>
        /// Reset.
        /// </summary>
        /// <returns>This builder after clearing previously selected encoding options.</returns>
        IWebPEncoderBuilder Reset();

        /// <summary>
        /// Make WeP encoder instance.
        /// </summary>
        /// <returns>Web encoder instance</returns>
        IWebPEncoder Build();

        /// <summary>
        /// Get current CLI arguments.
        /// </summary>
        /// <returns>CLI arguments</returns>
        string GetCurrentArguments();
    }
}