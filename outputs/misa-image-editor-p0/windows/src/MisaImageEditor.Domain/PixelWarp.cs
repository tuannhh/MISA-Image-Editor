namespace MisaImageEditor.Domain;

public static class PixelWarp
{
    public static byte[] ApplyRadialDistortion(byte[] pixels, int width, int height, double amount)
    {
        if (pixels.Length != checked(width * height * 4)) throw new ArgumentException("BGRA32 buffer has an unexpected length", nameof(pixels));
        if (width <= 0 || height <= 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (Math.Abs(amount) < 0.001) return pixels;
        var result = new byte[pixels.Length];
        var centerX = (width - 1) / 2.0;
        var centerY = (height - 1) / 2.0;
        var radiusX = Math.Max(1.0, centerX);
        var radiusY = Math.Max(1.0, centerY);
        var strength = Math.Clamp(amount / 100.0, -1.0, 1.0) * 0.35;
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var nx = (x - centerX) / radiusX;
                var ny = (y - centerY) / radiusY;
                var radiusSquared = Math.Min(1.0, nx * nx + ny * ny);
                var scale = 1.0 + strength * radiusSquared;
                var sourceX = Math.Clamp(centerX + nx * scale * radiusX, 0, width - 1.0);
                var sourceY = Math.Clamp(centerY + ny * scale * radiusY, 0, height - 1.0);
                var left = (int)Math.Floor(sourceX);
                var top = (int)Math.Floor(sourceY);
                var right = Math.Min(width - 1, left + 1);
                var bottom = Math.Min(height - 1, top + 1);
                var xWeight = sourceX - left;
                var yWeight = sourceY - top;
                var target = (y * width + x) * 4;
                for (var channel = 0; channel < 3; channel++)
                {
                    var topLeft = pixels[(top * width + left) * 4 + channel];
                    var topRight = pixels[(top * width + right) * 4 + channel];
                    var bottomLeft = pixels[(bottom * width + left) * 4 + channel];
                    var bottomRight = pixels[(bottom * width + right) * 4 + channel];
                    var topValue = topLeft + (topRight - topLeft) * xWeight;
                    var bottomValue = bottomLeft + (bottomRight - bottomLeft) * xWeight;
                    result[target + channel] = (byte)Math.Round(topValue + (bottomValue - topValue) * yWeight);
                }
                result[target + 3] = pixels[(top * width + left) * 4 + 3];
            }
        }
        return result;
    }
}
