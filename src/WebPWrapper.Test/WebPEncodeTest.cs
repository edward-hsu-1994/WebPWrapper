using System;
using System.Drawing;
using System.IO;
using WebPWrapper;
using WebPWrapper.Encoder;
using WebPWrapper.Decoder;
using Xunit;

namespace WebPWrapper.Test
{
    [Trait("Category", "Integration")]
    [Collection("WebP Init")]
    public class WebPEncodeTest
    {
        public WebPEncodeTest(WebPExecuteDownloadHelper fixture)
        {
        }

        [Fact]
        public void Encode_ConfiguredCropAndResize_ProducesWebPWithExpectedDimensions()
        {
            using var temporary = new TemporaryTestDirectory();
            var encoder = new WebPEncoderBuilder()
                .Crop(10, 10, 20, 20)
                .CopyMetadata(Metadatas.EXIF, Metadatas.ICC)
                .LowMemory()
                .MultiThread()
                .Resize(200, 200)
                .AlphaConfig(x => x.TransparentProcess(TransparentProcesses.Blend, Color.Black))
                .CompressionConfig(x => x.NearLossless(80, c => c.Size(1024 * 4)))
                .Build();

            Encode(encoder, "kaohsiung.jpg", temporary.OutputPath("configured.webp"));
            AssertWebP(temporary.OutputPath("configured.webp"), 200, 200);
        }

        [Fact]
        public void Encode_YellowTransparencyAndLossless_ProducesExpectedDimensions()
        {
            using var temporary = new TemporaryTestDirectory();
            var encoder = new WebPEncoderBuilder()
                .CopyMetadata(Metadatas.EXIF, Metadatas.ICC)
                .Resize(100, 100)
                .AlphaConfig(x => x.TransparentProcess(TransparentProcesses.Blend, Color.Yellow))
                .CompressionConfig(x => x.Lossless(y => y.Quality(75)))
                .Build();

            Encode(encoder, "openCC.png", temporary.OutputPath("yellow.webp"));
            AssertWebP(temporary.OutputPath("yellow.webp"), 100, 100);
        }

        [Fact]
        public void Encode_ExactTransparencyAndNearLossless_ProducesExpectedDimensions()
        {
            using var temporary = new TemporaryTestDirectory();
            var encoder = new WebPEncoderBuilder()
                .Resize(100, 100)
                .AlphaConfig(x => x.TransparentProcess(TransparentProcesses.Exact))
                .CompressionConfig(x => x.NearLossless(80, y => y.Size(1500)))
                .Build();

            Encode(encoder, "openCC.png", temporary.OutputPath("exact.webp"));
            AssertWebP(temporary.OutputPath("exact.webp"), 100, 100);
        }

        [Fact]
        public void Encode_LastResizeConfigurationWins_ProducesExpectedDimensions()
        {
            using var temporary = new TemporaryTestDirectory();
            var encoder = new WebPEncoderBuilder().Resize(100, 100).Resize(50, 0).Build();

            Encode(encoder, "openCC.png", temporary.OutputPath("reset-resize.webp"));
            AssertWebP(temporary.OutputPath("reset-resize.webp"), 50, 50);
        }

        [Fact]
        public void Encode_ZeroWidthResize_UsesRequestedHeightAndPreservesAspectRatio()
        {
            using var temporary = new TemporaryTestDirectory();
            var encoder = new WebPEncoderBuilder()
                .Resize(0, 600)
                .CompressionConfig(x => x.Lossy(y => y.Quality(80)))
                .Build();

            Encode(encoder, "openCC.png", temporary.OutputPath("lossy-resize.webp"));
            AssertWebP(temporary.OutputPath("lossy-resize.webp"), 600, 600);
        }

        [Fact]
        public void Encode_LossyWithoutResize_PreservesInputDimensions()
        {
            using var temporary = new TemporaryTestDirectory();
            var encoder = new WebPEncoderBuilder().CompressionConfig(x => x.Lossy(y => y.Quality(10))).Build();

            Encode(encoder, "kaohsiung.jpg", temporary.OutputPath("lossy.webp"));
            AssertWebP(temporary.OutputPath("lossy.webp"), 960, 639);
        }
        [Fact]
        public void Encode_InvalidInput_FailsInsteadOfProducingWebP()
        {
            using var temporary = new TemporaryTestDirectory();
            var outputPath = temporary.OutputPath("invalid.webp");
            using var input = new MemoryStream(new byte[] { 0, 1, 2, 3, 4 });
            using (var output = File.Create(outputPath))
            {
                var exception = Assert.Throws<ProcessExecutionException>(() => new WebPEncoderBuilder().Build().Encode(input, output));
                Assert.Equal(ProcessFailureKind.NonZeroExit, exception.Kind);
            }

            Assert.Equal(0, new FileInfo(outputPath).Length);
        }

        private static void Encode(IWebPEncoder encoder, string sampleName, string outputPath)
        {
            using (var input = File.OpenRead(SamplePath(sampleName)))
            using (var output = File.Create(outputPath))
                encoder.Encode(input, output);
        }

        private static string SamplePath(string sampleName) => Path.Combine(AppContext.BaseDirectory, "Samples", sampleName);

        private static void AssertWebP(string path, int width, int height)
        {
            var encoded = File.ReadAllBytes(path);
            Assert.True(encoded.Length > 12);
            Assert.Equal("RIFF", System.Text.Encoding.ASCII.GetString(encoded, 0, 4));
            Assert.Equal("WEBP", System.Text.Encoding.ASCII.GetString(encoded, 8, 4));

            using var temporary = new TemporaryTestDirectory();
            var decodedPath = temporary.OutputPath("roundtrip.png");
            using (var input = File.OpenRead(path))
            using (var output = File.Create(decodedPath))
                new WebPDecoderBuilder().Build().Decode(input, output);

            var decoded = File.ReadAllBytes(decodedPath);
            Assert.True(decoded.Length >= 24);
            Assert.Equal(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }, new byte[]
            {
                decoded[0], decoded[1], decoded[2], decoded[3], decoded[4], decoded[5], decoded[6], decoded[7]
            });
            Assert.Equal(width, ReadBigEndianInt32(decoded, 16));
            Assert.Equal(height, ReadBigEndianInt32(decoded, 20));
        }

        private static int ReadBigEndianInt32(byte[] bytes, int offset) =>
            (bytes[offset] << 24) | (bytes[offset + 1] << 16) | (bytes[offset + 2] << 8) | bytes[offset + 3];
    }
}
