using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using WebPWrapper;
using WebPWrapper.Encoder;
using Xunit;

namespace WebPWrapper.Test {
    public class WebPTest {
        [Fact]
        public void Case1() {
            WebPExecuteDownloader.Download();

            var builder = new WebPEncoderBuilder();

            var encoder = builder
                .Crop(10, 10, 20, 20)
                .CopyMetadata(Metadatas.EXIF, Metadatas.ICC)
                .LowMemory()
                .MultiThread()
                .Resize(200, 200)
                .AlphaConfig(x => x
                    .TransparentProcess(
                        TransparentProcesses.Blend,
                        Color.Black
                    )
                )
                .CompressionConfig(x => x.NearLossless(80, c => c.Size(1024 * 4)))
                .Build();

            var output = new MemoryStream();
            using (var inputFile = File.Open("Samples/kaohsiung.jpg", FileMode.Open)) {
                encoder.Encode(inputFile, output);
            }

            Assert.NotEqual(0, output.Length);
        }

        [Fact]
        public void Case2() {
            WebPExecuteDownloader.Download();

            var builder = new WebPEncoderBuilder();

            var encoder = builder
                .CopyMetadata(Metadatas.EXIF, Metadatas.ICC)
                .Resize(100, 100)
                .AlphaConfig(x => x
                    .TransparentProcess(
                        TransparentProcesses.Blend,
                        Color.Yellow
                    )
                )
                .CompressionConfig(x => x.Lossless(y => y.Quality(75)))
                .Build();

            if (!Directory.Exists("Output")) {
                Directory.CreateDirectory("Output");
            }

            using (var outputFile = File.Open("Output/openCC-Yellow.webp", FileMode.Create))
            using (var inputFile = File.Open("Samples/openCC.png", FileMode.Open)) {
                encoder.Encode(inputFile, outputFile);
            }
        }

        [Fact]
        public void Case3() {
            WebPExecuteDownloader.Download();

            var builder = new WebPEncoderBuilder();

            var encoder = builder
                .Resize(100, 100)
                .AlphaConfig(x => x
                    .TransparentProcess(
                        TransparentProcesses.Exact
                    )
                )
                .CompressionConfig(x => x.NearLossless(80, y => y.Size(1500)))
                .Build();

            if (!Directory.Exists("Output")) {
                Directory.CreateDirectory("Output");
            }

            using (var outputFile = File.Open("Output/openCC-Yellow2.webp", FileMode.Create))
            using (var inputFile = File.Open("Samples/openCC.png", FileMode.Open)) {
                encoder.Encode(inputFile, outputFile);
            }
        }
    }
}
