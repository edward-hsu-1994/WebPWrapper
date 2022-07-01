using System;
using System.Linq.Expressions;

namespace WebPWrapper.Decoder {
    /// <summary>
    /// WebP decoder builder
    /// </summary>
    public interface IWebPDecoderBuilder {
        /// <summary>
        /// Output image format
        /// </summary>
        /// <param name="format">Image format</param> 
        IWebPDecoderBuilder ExportFormat(ExportFormats format);

        /// <summary>
        /// Crop the decoded picture to a rectangle with top-left corner at coordinates (<paramref name="x"/>, <paramref name="y"/>) and size width x height. This cropping area must be fully contained within the source rectangle. The top-left corner will be snapped to even coordinates if needed. This option is meant to reduce the memory needed for cropping large images. Note: the cropping is applied before any scaling.
        /// </summary>
        /// <param name="x">Top-left corner X</param>
        /// <param name="y">Top-left corner Y</param>
        /// <param name="width">Width</param>
        /// <param name="height">Height</param> 
        IWebPDecoderBuilder Crop(int x, int y, int width, int height);

        /// <summary>
        /// Rescale the decoded picture. At least one of <paramref name="height"/> or <paramref name="width"/> is non-zero, and if one of the values is zero then the scaling is equal
        /// </summary>
        /// <param name="width">Width</param>
        /// <param name="height">Height</param> 
        IWebPDecoderBuilder Resize(int width, int height);

        /// <summary>
        /// Use multi-threading for decoding, if possible.
        /// </summary>
        IWebPDecoderBuilder MultiThread();

        /// <summary>
        /// Disable all assembly optimizations.
        /// </summary>
        IWebPDecoderBuilder DisableAssemblyOptimization();

        /// <summary>
        /// Don't use the in-loop filtering process even if it is required by the bitstream. This may produce visible blocks on the non-compliant output, but it will make the decoding faster.
        /// </summary> 
        IWebPDecoderBuilder NoFilter();

        /// <summary>
        /// Don't use the fancy upscaler for YUV420. This may lead to jaggy edges (especially the red ones), but should be faster.
        /// </summary>
        IWebPDecoderBuilder NoFancy();

        /// <summary>
        /// Specify a dithering strength between 0 and 100. Dithering is a post-processing effect applied to chroma components in lossy compression. It helps by smoothing gradients and avoiding banding artifacts.
        /// </summary>
        /// <param name="strength">Dithering strength(0~100)</param> 
        IWebPDecoderBuilder Dither(int strength);

        /// <summary>
        /// Flip decoded image vertically (can be useful for OpenGL textures for instance).
        /// </summary>
        IWebPDecoderBuilder Flip();

        /// <summary>
        /// Reset
        /// </summary>
        IWebPDecoderBuilder Reset();

        /// <summary>
        /// Make WeP Decoder instance.
        /// </summary>
        /// <returns>Web Decoder instance</returns>
        IWebPDecoder Build();

        /// <summary>
        /// Get current CLI arguments.
        /// </summary>
        /// <returns>CLI arguments</returns>
        string GetCurrentArguments();
    }
}