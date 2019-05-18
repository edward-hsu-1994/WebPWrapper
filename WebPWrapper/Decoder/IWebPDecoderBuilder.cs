using System;
using System.Linq.Expressions;

namespace WebPWrapper.Decoder {
    public interface IWebPDecoderBuilder {
        /// <summary>
        /// 輸出格式
        /// </summary>
        /// <param name="format">格式</param> 
        IWebPDecoderBuilder ExportFormat(ExportFormats format);

        /// <summary>
        /// 輸入圖片裁減
        /// </summary>
        /// <param name="x">起始座標X</param>
        /// <param name="y">起始座標Y</param>
        /// <param name="width">寬度</param>
        /// <param name="height">高度</param> 
        IWebPDecoderBuilder Crop(int x, int y, int width, int height);

        /// <summary>
        /// 縮放圖片，<paramref name="height"/>與<paramref name="width"/>至少一者非0，如果其中一值為0則等比例縮放
        /// </summary>
        /// <param name="width">寬度</param>
        /// <param name="height">寬度</param> 
        IWebPDecoderBuilder Resize(int width, int height);

        /// <summary>
        /// 容許多執行序
        /// </summary>
        IWebPDecoderBuilder MultiThread();

        /// <summary>
        /// 停用ASM優化
        /// </summary>
        IWebPDecoderBuilder DisableAssemblyOptimization();

        /// <summary>
        /// 禁止濾波過程
        /// </summary> 
        IWebPDecoderBuilder NoFilter();

        /// <summary>
        /// 禁止YUV420升級器
        /// </summary>
        IWebPDecoderBuilder NoFancy();

        /// <summary>
        /// 抖動強度，抖動是應用於有損壓縮中的色度分量的後處理效果。它有助於平滑漸變並避免條帶偽影
        /// </summary>
        /// <param name="strength">強度，最小0，最大100</param> 
        IWebPDecoderBuilder Dither(int strength);

        /// <summary>
        /// 垂直翻轉
        /// </summary>
        /// <returns></returns>
        IWebPDecoderBuilder Flip();

        /// <summary>
        /// 重設回預設值
        /// </summary>
        IWebPDecoderBuilder Reset();

        /// <summary>
        /// 建構WebP解碼器
        /// </summary>
        /// <returns>WebP解碼器</returns>
        IWebPDecoder Build();

        /// <summary>
        /// 取得目前CLI參數
        /// </summary>
        /// <returns>CLI參數</returns>
        string GetCurrentArguments();
    }
}