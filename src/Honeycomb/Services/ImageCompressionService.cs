using System;
using System.IO;
using SkiaSharp;

namespace Honeycomb.Services;

public class ImageCompressionService
{
    private const int MaxEdge = 1024;
    private const int JpegQuality = 80;

    /// <summary>解碼 → 等比縮放（只縮不放）→ 重新編碼為 JPEG。</summary>
    /// <exception cref="InvalidOperationException">來源無法解碼為圖片時。</exception>
    public byte[] Compress(Stream source)
    {
        using var original = SKBitmap.Decode(source);
        if (original is null)
            throw new InvalidOperationException("無法讀取圖片");

        var longestEdge = Math.Max(original.Width, original.Height);

        SKBitmap toEncode = original;
        SKBitmap? resized = null;
        if (longestEdge > MaxEdge)
        {
            var scale = (float)MaxEdge / longestEdge;
            var width = Math.Max(1, (int)Math.Round(original.Width * scale));
            var height = Math.Max(1, (int)Math.Round(original.Height * scale));
            resized = original.Resize(new SKImageInfo(width, height), SKFilterQuality.High)
                      ?? throw new InvalidOperationException("圖片縮放失敗");
            toEncode = resized;
        }

        try
        {
            using var image = SKImage.FromBitmap(toEncode);
            using var data = image.Encode(SKEncodedImageFormat.Jpeg, JpegQuality);
            return data.ToArray();
        }
        finally
        {
            resized?.Dispose();
        }
    }
}
