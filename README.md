# WebPWrapper

這是一個可以在`.NET Standard 2.0`環境運行的WebP包裝套件。提供包裝好的CLI參數建構器幫助調用WebP CLI。

有關WebP詳細資訊請參考官方網站: [https://developers.google.com/speed/webp/](https://developers.google.com/speed/webp/)

## 快速上手

### 安裝套件
```shell
dotnet add package WebPWrapper
```

### 編碼
```csharp 
using WebPWrapper.Encoder;

var builder = new WebPEncoderBuilder();

var encoder = builder
	.Resize(100, 0) // 調整寬度為100，等比縮放(因為高度為0)
	.AlphaConfig(x => x // 透明處理設定
		.TransparentProcess(
			TransparentProcesses.Blend, // 透明部分將底色視為黃色混合
			Color.Yellow
		)
	)
	.CompressionConfig(x => x // 壓縮設定
		.Lossless(y => y.Quality(75)) // 使用無損壓縮且壓縮品質設為75
	) 
	.Build(); // 建立編碼器

using (var outputFile = File.Open("output.webp", FileMode.Create))
using (var inputFile = File.Open("input.png", FileMode.Open)) {
	encoder.Encode(inputFile, outputFile); // 編碼
}
```

### 解碼
```csharp
using WebPWrapper.Decoder; 

var builder = new WebPDecoderBuilder();

var encoder = builder
	.Resize(32, 0) // 調整寬度為32，等比縮放(因為高度為0)
	.Build(); // 建立解碼器
 
using (var outputFile = File.Open("output.png", FileMode.Create))
using (var inputFile = File.Open("input.webp", FileMode.Open)) {
	encoder.Decode(inputFile, outputFile); // 解碼
}
```
