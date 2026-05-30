using Honeycomb.Services;
using SkiaSharp;

namespace Honeycomb.Tests.Services;

public class ImageCompressionServiceTests
{
    private readonly ImageCompressionService _service = new();

    private static byte[] CreateImageBytes(int width, int height)
    {
        var info = new SKImageInfo(width, height);
        using var surface = SKSurface.Create(info);
        surface.Canvas.Clear(SKColors.CornflowerBlue);
        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }

    private static (int Width, int Height) Dimensions(byte[] bytes)
    {
        using var bmp = SKBitmap.Decode(bytes);
        return (bmp.Width, bmp.Height);
    }

    [Fact]
    public void Compress_DownscalesLargeImage_ToMaxEdge()
    {
        var input = CreateImageBytes(4000, 3000);

        var output = _service.Compress(new MemoryStream(input));

        var (w, h) = Dimensions(output);
        Assert.True(Math.Max(w, h) <= 1024, $"longest edge was {Math.Max(w, h)}");
        Assert.Equal(1024, w); // 4000 是長邊，縮到 1024
    }

    [Fact]
    public void Compress_DoesNotUpscaleSmallImage()
    {
        var input = CreateImageBytes(500, 400);

        var output = _service.Compress(new MemoryStream(input));

        var (w, h) = Dimensions(output);
        Assert.Equal(500, w);
        Assert.Equal(400, h);
    }

    [Fact]
    public void Compress_OutputIsJpeg()
    {
        var input = CreateImageBytes(800, 600);

        var output = _service.Compress(new MemoryStream(input));

        Assert.True(output.Length >= 3);
        Assert.Equal(0xFF, output[0]);
        Assert.Equal(0xD8, output[1]);
        Assert.Equal(0xFF, output[2]);
    }

    [Fact]
    public void Compress_InvalidBytes_Throws()
    {
        var garbage = new byte[] { 1, 2, 3, 4, 5 };

        Assert.Throws<InvalidOperationException>(
            () => _service.Compress(new MemoryStream(garbage)));
    }
}
