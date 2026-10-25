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

## Supported platforms

The library targets .NET Standard 2.1. The bundled WebP CLI downloader supports:

| Operating system | Architecture |
| --- | --- |
| Windows | x64 |
| Linux | x64 |
| macOS | x64, ARM64 |

Other platforms or architectures must provide compatible `cwebp` and `dwebp` executables through the builder constructors.

## Containerized applications

For containerized applications, install the WebP CLI in the image instead of downloading it at runtime. For example, in a Debian-based image:

```dockerfile
RUN apt-get update \
    && apt-get install -y --no-install-recommends webp \
    && rm -rf /var/lib/apt/lists/*
```

Pass the executable paths supplied by the image to the builders. On Debian-based images these are typically `/usr/bin/cwebp` and `/usr/bin/dwebp`; verify paths for the selected base image:

```csharp
var encoder = new WebPEncoderBuilder("/usr/bin/cwebp").Build();
var decoder = new WebPDecoderBuilder("/usr/bin/dwebp").Build();
```

This example depends on the package name and CLI paths provided by the chosen image repository. Pin the base image and package versions according to your deployment policy.

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
