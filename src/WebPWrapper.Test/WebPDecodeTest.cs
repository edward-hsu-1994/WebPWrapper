using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using WebPWrapper;
using WebPWrapper.Decoder;
using WebPWrapper.Encoder;
using Xunit;

namespace WebPWrapper.Test {
    [Collection("WebP Init")]
    public class WebPDecodeTest {
        public WebPDecodeTest(WebPExecuteDownloadHelper a) {

        }

        [Fact]
        public void Case1() {
            var builder = new WebPDecoderBuilder();

            var encoder = builder
                .Flip()
                .Build();

            var output = new MemoryStream();
            using (var inputFile = File.Open("Samples/openCC.webp", FileMode.Open)) {
                encoder.Decode(inputFile, output);
            }

            Assert.NotEqual(0, output.Length);
        }

        [Fact]
        public void Case2() {
            var builder = new WebPDecoderBuilder();

            var encoder = builder
                .Flip()
                .Build();

            if (!Directory.Exists("Output")) {
                Directory.CreateDirectory("Output");
            }

            using (var outputFile = File.Open("Output/openCC-Flip.png", FileMode.Create))
            using (var inputFile = File.Open("Samples/openCC.webp", FileMode.Open)) {
                encoder.Decode(inputFile, outputFile);
            }
        }

        [Fact]
        public void Case3() {
            var builder = new WebPDecoderBuilder();

            var encoder = builder
                .Flip()
                .Resize(32, 0)
                .Build();

            if (!Directory.Exists("Output")) {
                Directory.CreateDirectory("Output");
            }

            using (var outputFile = File.Open("Output/openCC-Resize.png", FileMode.Create))
            using (var inputFile = File.Open("Samples/openCC.webp", FileMode.Open)) {
                encoder.Decode(inputFile, outputFile);
            }
        }


        [Fact]
        public void Case4() {
            var builder = new WebPDecoderBuilder();

            var encoder = builder
                .Crop(0, 0, 50, 50)
                .MultiThread()
                .Build();

            if (!Directory.Exists("Output")) {
                Directory.CreateDirectory("Output");
            }

            using (var outputFile = File.Open("Output/openCC-Crop.png", FileMode.Create))
            using (var inputFile = File.Open("Samples/openCC.webp", FileMode.Open)) {
                encoder.Decode(inputFile, outputFile);
            }
        }

        [Fact]
        public void Case5() {
            var builder = new WebPDecoderBuilder();

            var encoder = builder
                .Crop(0, 0, 50, 50)
                .ExportFormat(ExportFormats.BMP)
                .Build();

            if (!Directory.Exists("Output")) {
                Directory.CreateDirectory("Output");
            }

            using (var outputFile = File.Open("Output/openCC.bmp", FileMode.Create))
            using (var inputFile = File.Open("Samples/openCC.webp", FileMode.Open)) {
                encoder.Decode(inputFile, outputFile);
            }
        }

        [Fact]
        public void Case6() {
            var builder = new WebPDecoderBuilder();

            var encoder = builder
                .Crop(0, 0, 50, 50)
                .ExportFormat(ExportFormats.BMP)
                .ExportFormat(ExportFormats.TIFF)
                .Build();

            if (!Directory.Exists("Output")) {
                Directory.CreateDirectory("Output");
            }

            using (var outputFile = File.Open("Output/openCC.tiff", FileMode.Create))
            using (var inputFile = File.Open("Samples/openCC.webp", FileMode.Open)) {
                encoder.Decode(inputFile, outputFile);
            }
        }
    }
}
