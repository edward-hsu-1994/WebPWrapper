using System.IO;
using System.Text;
using WebPWrapper.Encoder;
using Xunit;

namespace WebPWrapper.Test {
    [Collection("WebP Init")]
    public class AlphaConfigurationTests {
        [Fact]
        public void DisableCompression_EncodesWithFollowingAlphaOption() {
            var encoder = new WebPEncoderBuilder()
                .AlphaConfig(alpha => alpha
                    .DisableCompression()
                    .Filter(AlphaFilters.None))
                .Build();

            using var input = File.OpenRead("Samples/openCC.png");
            using var output = new MemoryStream();
            encoder.Encode(input, output);

            var encoded = output.ToArray();
            Assert.Equal("RIFF", Encoding.ASCII.GetString(encoded, 0, 4));
            Assert.Equal("WEBP", Encoding.ASCII.GetString(encoded, 8, 4));
        }
    }
}
