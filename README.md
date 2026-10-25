# WebPWrapper

[![Unit Test](https://github.com/XuPeiYao/WebPWrapper/actions/workflows/unit-test.yml/badge.svg?branch=master)](https://github.com/XuPeiYao/WebPWrapper/actions/workflows/unit-test.yml) [![NuGet Version](https://img.shields.io/nuget/v/WebPWrapper.svg)](#) [![NuGet Download](https://img.shields.io/nuget/dt/WebPWrapper.svg)](https://www.nuget.org/packages/WebPWrapper/) [![Github license](https://img.shields.io/github/license/XuPeiYao/WebPWrapper.svg)](#)

This library is a wrapper for WebP CLI. 
Provides a simple CLI parameter builder, making it easier to use WebP in development

For more information about WebP and WebP CLI, please refer to [this website](https://developers.google.com/speed/webp/).

## Getting started

### Install library
```shell
dotnet add package WebPWrapper
```

### Use case
```csharp 
using WebPWrapper;
using WebPWrapper.Encoder;

WebPExecuteDownloader.Download();

var builder = new WebPEncoderBuilder();

var encoder = builder
	.Resize(100, 0) // Resize image to 100px (length)
	.AlphaConfig(x => x // set alpha config
		.TransparentProcess(
			TransparentProcesses.Blend, // Change transparent color to blend with yellow color
			Color.Yellow
		)
	)
	.CompressionConfig(x => x // set compression config
		.Lossless(y => y.Quality(75)) // set lossless config
	) 
	.Build(); // build encoder

using (var outputFile = File.Open("output.webp", FileMode.Create))
using (var inputFile = File.Open("input.png", FileMode.Open)) {
	encoder.Encode(inputFile, outputFile); // encode image
}
```

## CLI downloads

`WebPExecuteDownloader.Download()` downloads libwebp 1.6.0, verifies the platform archive against its pinned SHA-256 digest, applies archive size and path-safety limits, and installs the CLI through a staged directory. For checksum provenance, limits, and failure behavior, see [CLI download security](docs/security/cli-downloads.md).

## Tests

Run the deterministic, offline test suite with:

```shell
dotnet test src/WebPWrapper.sln
```

Tests that download and exercise the official WebP CLI are isolated in the integration test project. Run them explicitly with:

```shell
dotnet test src/WebPWrapper.IntegrationTest/WebPWrapper.IntegrationTest.csproj
```

## Suggestions

For containerized services, I do not recommend downloading CLI in Runtime.
I recommend installing the CLI when building the docker image. Then set `executeFilePath` in builder class's constructs
