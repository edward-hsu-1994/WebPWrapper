# WebPWrapper

[![Build Status](https://jenkins.gofa.cloud/buildStatus/icon?job=WebPWrapper)](#) [![NuGet Version](https://img.shields.io/nuget/v/WebPWrapper.svg)](#) [![NuGet Download](https://img.shields.io/nuget/dt/WebPWrapper.svg)](#) [![Github license](https://img.shields.io/github/license/XuPeiYao/WebPWrapper.svg)](#)

這是一個可以在`.NET Standard 2.0`環境運行的WebP包裝套件。提供包裝好的CLI參數建構器幫助調用WebP CLI。

有關WebP詳細資訊請參考官方網站: [https://developers.google.com/speed/webp/](https://developers.google.com/speed/webp/)

## 快速上手

### 安裝套件
```shell
dotnet add package WebPWrapper
```

### 編碼
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