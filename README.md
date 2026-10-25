# WebPWrapper

[![Unit Test](https://github.com/edward-hsu-1994/WebPWrapper/actions/workflows/unit-test.yml/badge.svg?branch=master)](https://github.com/edward-hsu-1994/WebPWrapper/actions/workflows/unit-test.yml) [![NuGet Version](https://img.shields.io/nuget/v/WebPWrapper.svg)](https://www.nuget.org/packages/WebPWrapper/) [![NuGet Download](https://img.shields.io/nuget/dt/WebPWrapper.svg)](https://www.nuget.org/packages/WebPWrapper/) [![GitHub license](https://img.shields.io/github/license/edward-hsu-1994/WebPWrapper.svg)](https://github.com/edward-hsu-1994/WebPWrapper/blob/master/LICENSE)

WebPWrapper is a .NET library that builds and runs `cwebp` and `dwebp` command-line operations through stream-based encoder and decoder APIs.

For more information about WebP and the WebP CLI, see the [WebP documentation](https://developers.google.com/speed/webp/).

## Getting started

### Install the library

```shell
dotnet add package WebPWrapper
```

### Encode an image

```csharp
using System.Drawing;
using System.IO;
using WebPWrapper;
using WebPWrapper.Encoder;

WebPExecuteDownloader.Download();

var encoder = new WebPEncoderBuilder()
    .Resize(100, 0)
    .AlphaConfig(config => config.TransparentProcess(
        TransparentProcesses.Blend,
        Color.Yellow))
    .CompressionConfig(config => config.Lossless(lossless => lossless.Quality(75)))
    .Build();

using (var input = File.OpenRead("input.png"))
using (var output = File.Create("output.webp"))
{
    encoder.Encode(input, output);
}
```

`WebPExecuteDownloader.Download()` installs the platform's WebP CLI tools before the builder uses them. Applications that manage their own executables can pass the executable path to `WebPEncoderBuilder` or `WebPDecoderBuilder` instead.

## Containerized applications

For containerized applications, avoid downloading the CLI at runtime. Install it while building the image, then pass its executable path to the `WebPEncoderBuilder` and `WebPDecoderBuilder` constructors.

## Tests

Run the deterministic, offline test suite with:

```shell
dotnet test src/WebPWrapper.sln
```

Tests that download and exercise the official WebP CLI are isolated in the integration test project. Run them explicitly with:

```shell
dotnet test src/WebPWrapper.IntegrationTest/WebPWrapper.IntegrationTest.csproj
```

## Documentation

- [Usage and API overview](docs/usage-and-api.md): stream encoding/decoding, async cancellation, builder options, and public type responsibilities.
- [CLI download security](docs/security/cli-downloads.md): integrity verification, extraction limits, installation, and failure behavior.
- [NuGet.org publishing](docs/release-publishing.md): Trusted Publishing setup and the tag-based release process.
