using System;
using System.IO;
using WebPWrapper.Decoder;
using Xunit;

namespace WebPWrapper.Test
{
    [Trait("Category", "Integration")]
    [Collection("WebP Init")]
    public class WebPDecodeTest
    {
        public WebPDecodeTest(WebPExecuteDownloadHelper fixture)
        {
        }

        [Fact]
        public void Decode_Flip_ProducesExpectedPngDimensions()
        {
            using var temporary = new TemporaryTestDirectory();
            var outputPath = temporary.OutputPath("flip.png");
            Decode("openCC.webp", outputPath, new WebPDecoderBuilder().Flip().Build());
            AssertPng(outputPath, 100, 100);
        }

        [Fact]
        public void Decode_Resize_ProducesRequestedDimensions()
        {
            using var temporary = new TemporaryTestDirectory();
            var outputPath = temporary.OutputPath("resize.png");
            Decode("openCC.webp", outputPath, new WebPDecoderBuilder().Resize(32, 0).Build());
            AssertPng(outputPath, 32, 32);
        }

        [Fact]
        public void Decode_Crop_ProducesRequestedDimensions()
        {
            using var temporary = new TemporaryTestDirectory();
            var outputPath = temporary.OutputPath("crop.png");
            Decode("openCC.webp", outputPath, new WebPDecoderBuilder().Crop(0, 0, 50, 50).MultiThread().Build());
            AssertPng(outputPath, 50, 50);
        }

        [Fact]
        public void Decode_ExportFormatBmp_ProducesBmpWithExpectedDimensions()
        {
            using var temporary = new TemporaryTestDirectory();
            var outputPath = temporary.OutputPath("crop.bmp");
            Decode("openCC.webp", outputPath,
                new WebPDecoderBuilder().Crop(0, 0, 50, 50).ExportFormat(ExportFormats.BMP).Build());

            var output = File.ReadAllBytes(outputPath);
            Assert.True(output.Length >= 26);
            Assert.Equal((byte)'B', output[0]);
            Assert.Equal((byte)'M', output[1]);
            Assert.Equal(50, BitConverter.ToInt32(output, 18));
            Assert.Equal(50, Math.Abs(BitConverter.ToInt32(output, 22)));
        }

        [Fact]
        public void Decode_ExportFormatTiff_ProducesTiffFile()
        {
            using var temporary = new TemporaryTestDirectory();
            var outputPath = temporary.OutputPath("crop.tiff");
            Decode("openCC.webp", outputPath,
                new WebPDecoderBuilder().Crop(0, 0, 50, 50).ExportFormat(ExportFormats.BMP).ExportFormat(ExportFormats.TIFF).Build());

            var output = File.ReadAllBytes(outputPath);
            Assert.True(output.Length > 8);
            Assert.True((output[0] == (byte)'I' && output[1] == (byte)'I' && output[2] == 42 && output[3] == 0) ||
                        (output[0] == (byte)'M' && output[1] == (byte)'M' && output[2] == 0 && output[3] == 42));
        }

        [Fact]
        public void Decode_InvalidInput_FailsInsteadOfProducingAnImage()
        {
            using var temporary = new TemporaryTestDirectory();
            var outputPath = temporary.OutputPath("invalid.png");
            using var input = new MemoryStream(new byte[] { 0, 1, 2, 3, 4 });
            using (var output = File.Create(outputPath))
                Assert.Throws<Exception>(() => new WebPDecoderBuilder().Build().Decode(input, output));

            Assert.Equal(0, new FileInfo(outputPath).Length);
        }

        private static void Decode(string sampleName, string outputPath, IWebPDecoder decoder)
        {
            using (var input = File.OpenRead(SamplePath(sampleName)))
            using (var output = File.Create(outputPath))
                decoder.Decode(input, output);
        }

        private static string SamplePath(string sampleName) => Path.Combine(AppContext.BaseDirectory, "Samples", sampleName);

        private static void AssertPng(string path, int width, int height)
        {
            var output = File.ReadAllBytes(path);
            Assert.True(output.Length >= 24);
            Assert.Equal(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 },
                new byte[] { output[0], output[1], output[2], output[3], output[4], output[5], output[6], output[7] });
            Assert.Equal(width, ReadBigEndianInt32(output, 16));
            Assert.Equal(height, ReadBigEndianInt32(output, 20));
        }

        private static int ReadBigEndianInt32(byte[] bytes, int offset) =>
            (bytes[offset] << 24) | (bytes[offset + 1] << 16) | (bytes[offset + 2] << 8) | bytes[offset + 3];
    }
}
