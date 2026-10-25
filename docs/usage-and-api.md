# Usage and API overview

WebPWrapper is a .NET wrapper around the `cwebp` and `dwebp` command-line tools. It accepts .NET streams, stages their contents in temporary files for the CLI, and writes the result to the output stream. See [CLI download security](security/cli-downloads.md) for archive integrity, extraction safeguards, and installation behavior.

## Install

Add the NuGet package to your project:

```shell
dotnet add package WebPWrapper
```

The library resolves a default CLI path for the current supported platform, but does not download the CLI automatically. Install it once before building an encoder or decoder:

```csharp
using WebPWrapper;

await WebPExecuteDownloader.DownloadAsync();
```

For a synchronous application, call `WebPExecuteDownloader.Download()` instead. These methods use the per-user installation; the downloader's checksums and installation details are documented in [CLI download security](security/cli-downloads.md).

## Encode and decode streams

The encoder and decoder consume readable input streams and write to writable output streams. This synchronous example uses files, but the API accepts any compatible `Stream`:

```csharp
using System.IO;
using WebPWrapper.Encoder;
using WebPWrapper.Decoder;

var encoder = new WebPEncoderBuilder().Build();
using (var input = File.OpenRead("photo.png"))
using (var output = File.Create("photo.webp"))
{
    encoder.Encode(input, output);
}

var decoder = new WebPDecoderBuilder()
    .ExportFormat(ExportFormats.PNG)
    .Build();
using (var input = File.OpenRead("photo.webp"))
using (var output = File.Create("decoded.png"))
{
    decoder.Decode(input, output);
}
```

`ExportFormats.PNG` is the default decoder output format; it is included above to make the selected format explicit. The input is consumed from its current position to the end, and the output is written at its current position. The wrapper does not dispose the streams supplied by the caller.

## Async operations and cancellation
Both `IWebPEncoder` and `IWebPDecoder` provide asynchronous overloads with and without a `CancellationToken`. The overload without a token uses `CancellationToken.None`. The token-aware overload observes cancellation while copying streams and while the CLI process is running; cancellation of a running CLI terminates that process.

```csharp
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using WebPWrapper.Encoder;
using WebPWrapper.Decoder;

async Task ConvertAsync(CancellationToken cancellationToken)
{
    var encoder = new WebPEncoderBuilder().Build();
    using (var input = File.OpenRead("photo.png"))
    using (var output = File.Create("photo.webp"))
    {
        await encoder.EncodeAsync(input, output, cancellationToken);
    }

    var decoder = new WebPDecoderBuilder().Build();
    using (var input = File.OpenRead("photo.webp"))
    using (var output = File.Create("decoded.png"))
    {
        await decoder.DecodeAsync(input, output, cancellationToken);
    }
}
```

The examples assume the CLI was installed beforehand as described above. Cancellation and other CLI process failures are described under [process failures](#process-failures).

## Builder customization

Builders are fluent: configure options, then call `Build()` to get an encoder or decoder. Encoder settings include resize/crop, threading, metadata copying, presets, compression, and alpha options. Decoder settings include output format, resize/crop, threading, filtering, dithering, and vertical flipping.

```csharp
using WebPWrapper.Encoder;

var encoder = new WebPEncoderBuilder()
    .Resize(1200, 0) // preserve the aspect ratio while setting width to 1200
    .MultiThread()
    .CopyMetadata(Metadatas.EXIF, Metadatas.ICC)
    .CompressionConfig(config => config.Lossy(lossy => lossy.Quality(82)))
    .Build();
```

For lossless encoding, `CompressionConfig(config => config.Lossless(lossless => lossless.Mode(6)))` is also available. The configuration callbacks expose the corresponding configuration class methods; see [configuration and option types](#configuration-and-option-types). `Reset()` clears accumulated builder options, and `GetCurrentArguments()` returns the formatted CLI arguments currently accumulated by a builder.

## Supplying a CLI executable path

If the application manages its own `cwebp` and `dwebp` executables, pass the executable file path to the corresponding builder constructor. The path is used as supplied; it must identify an existing file when the builder is constructed.

```csharp
using WebPWrapper.Encoder;
using WebPWrapper.Decoder;

var encoder = new WebPEncoderBuilder("/opt/libwebp/bin/cwebp").Build();
var decoder = new WebPDecoderBuilder("/opt/libwebp/bin/dwebp").Build();
```

When no path is supplied, builders resolve the default executable location for the current platform; they do not download missing executables. Use `WebPExecuteDownloader.Download()` or `DownloadAsync()` for the default installation.

## API overview

Types live in the `WebPWrapper`, `WebPWrapper.Encoder`, and `WebPWrapper.Decoder` namespaces.

| API | Responsibility |
| --- | --- |
| `IWebPEncoder` | Encodes a readable stream to WebP through `Encode` or `EncodeAsync`; the async overload accepting `CancellationToken` supports cancellation. |
| `WebPEncoder` | Default implementation of `IWebPEncoder`, created by `WebPEncoderBuilder.Build()`. |
| `IWebPEncoderBuilder` / `WebPEncoderBuilder` | Fluent interface and default implementation for configuring encoding; `Build()` creates the encoder. `WebPEncoderBuilder` optionally accepts a `cwebp` path. |
| `IWebPDecoder` | Decodes a WebP stream to the selected output image format through `Decode` or `DecodeAsync`, including a cancellation-token overload. |
| `WebPDecoder` | Default implementation of `IWebPDecoder`, created by `WebPDecoderBuilder.Build()`. |
| `IWebPDecoderBuilder` / `WebPDecoderBuilder` | Fluent interface and default implementation for output format and decode options; `Build()` creates the decoder. `WebPDecoderBuilder` optionally accepts a `dwebp` path. |
| `CompressionConfiguration` | Selects lossless, near-lossless, or lossy compression and configures its settings inside `CompressionConfig(...)`. |
| `LosslessConfiguration` | Configures lossless mode, quality, and method. |
| `NearLosslessConfiguration` | Configures near-lossless preprocessing, quality, method, size/PSNR targets, and filtering. |
| `NearLosslessFilterConfiguration` | Configures filter options used by `NearLosslessConfiguration.Filter(...)`. |
| `LossyConfiguration` | Configures lossy quality, method, target size/PSNR, and related encoder settings. |
| `AlphaConfiguration` | Configures alpha compression/filtering and transparent-pixel processing. |
| `Metadatas` | Selects metadata categories (`All`, `EXIF`, `ICC`, or `XMP`) for `CopyMetadata(...)`. |
| `PresetProfiles` | Selects an encoder preset (`Default`, `Photo`, `Picture`, `Drawing`, `Icon`, or `Text`) for `LoadPresetProfile(...)`. |
| `AlphaFilters` | Selects the alpha-plane filtering strategy. |
| `TransparentProcesses` | Selects handling for transparent pixels during encoding. |
| `ExportFormats` | Selects decoded output format: `PNG`, `TIFF`, `PAM`, `PPM`, `PGM`, `YUV`, or `BMP`. |
| `WebPExecuteDownloader` | Installs the platform's WebP CLI tools with `Download()` or `DownloadAsync(bool ignoreIfExtsis = true)`. |
| `ProcessFailureKind` | Categorizes CLI process failures as `StartFailed`, `NonZeroExit`, `TimedOut`, or `Canceled`. |
| `ProcessExecutionException` | Provides process failure kind, executable path, optional exit code, and captured standard output/error. |
| `ProcessExecutionCanceledException` | An `OperationCanceledException` subtype with captured CLI failure details in `FailureDetails`. |

`CompressionConfiguration` and its nested configuration objects are supplied to the builder's configuration callbacks; their constructors are not the public entry point. `WebPDecoderBuilder.ExportFormat(ExportFormats.PNG)` selects PNG explicitly; selecting `ExportFormats.PNG` is equivalent to the CLI's default PNG output. The decoder builder translates `TIFF`, `PAM`, `PPM`, `PGM`, `YUV`, and `BMP` to their corresponding CLI switches.

### Process failures

Failures to start the CLI, nonzero CLI exits, and CLI timeouts are reported as `ProcessExecutionException`. Inspect `Kind`, `ExecutablePath`, `ExitCode` (when available), `StandardOutput`, and `StandardError` for diagnostics. When cancellation terminates a running process, the async call can throw `ProcessExecutionCanceledException`, which derives from `OperationCanceledException`; its `FailureDetails` property contains the captured `ProcessExecutionException`. A cancellation observed before process execution may instead throw the standard `OperationCanceledException` from the token.
