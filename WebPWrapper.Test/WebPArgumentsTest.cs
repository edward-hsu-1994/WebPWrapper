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

            //var output = new MemoryStream();
            //encoder.Encode(File.Open("test.png", FileMode.Open), output);
        }
    }
}
