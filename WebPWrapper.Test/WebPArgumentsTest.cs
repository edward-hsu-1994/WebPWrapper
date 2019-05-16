using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using WebPWrapper;
using WebPWrapper.Encoder;
using Xunit;

namespace WebPWrapper.Test {
    public class WebPArgumentsTest {
        [Fact]
        public void ArgumentCase1() {
            WebPExecuteDownloader.Download();

            var builder = new WebPEncoderBuilder();

            var encoder = builder
                .Crop(10, 10, 20, 20)
                .CopyMetadata(Metadatas.EXIF, Metadatas.ICC)
                .LowMemory()
                .MultiThread()
                .Resize(200, 200)
                .Build();

            var output = new MemoryStream();
            using (var inputFile = File.Open("Samples/kaohsiung.jpg", FileMode.Open)) {
                encoder.Encode(inputFile, output);
            }

            Assert.NotEqual(0, output.Length);
        }
    }
}
