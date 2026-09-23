namespace MisaImageEditor.Domain;

/// <summary>Normalizes a BGRA32 image from an EXIF orientation into top-left orientation.</summary>
public static class PixelOrientation
{
    public static (byte[] Pixels, int Width, int Height) NormalizeBgra32(byte[] pixels, int width, int height, int orientation)
    {
        if (width <= 0 || height <= 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (pixels.Length != checked(width * height * 4))
            throw new ArgumentException("BGRA32 buffer has an unexpected length", nameof(pixels));

        orientation = orientation is >= 1 and <= 8 ? orientation : 1;
        if (orientation == 1) return (pixels, width, height);

        var outputWidth = orientation >= 5 ? height : width;
        var outputHeight = orientation >= 5 ? width : height;
        var output = new byte[checked(outputWidth * outputHeight * 4)];
        for (var y = 0; y < outputHeight; y++)
        {
            for (var x = 0; x < outputWidth; x++)
            {
                var (sourceX, sourceY) = orientation switch
                {
                    2 => (width - 1 - x, y),
                    3 => (width - 1 - x, height - 1 - y),
                    4 => (x, height - 1 - y),
                    5 => (y, x),
                    6 => (y, height - 1 - x),
                    7 => (width - 1 - y, height - 1 - x),
                    8 => (width - 1 - y, x),
                    _ => (x, y)
                };
                Buffer.BlockCopy(pixels, (sourceY * width + sourceX) * 4, output, (y * outputWidth + x) * 4, 4);
            }
        }
        return (output, outputWidth, outputHeight);
    }
}
