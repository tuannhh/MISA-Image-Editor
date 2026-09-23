namespace MisaImageEditor.Domain;

/// <summary>Estimates a small corrective rotation from the strongest near-horizontal image edges.</summary>
public static class HorizonStraightener
{
    private const double MaximumAbsoluteAngle = 20.0;
    private const double BinSize = 0.5;

    public static bool TryEstimateCorrectionDegrees(byte[] bgra32, int width, int height, out double correctionDegrees)
    {
        correctionDegrees = 0;
        if (width < 8 || height < 8 || bgra32.Length != checked(width * height * 4)) return false;

        var step = Math.Max(1, (int)Math.Ceiling(Math.Max(width, height) / 900.0));
        var binCount = (int)(MaximumAbsoluteAngle * 2 / BinSize) + 1;
        var weights = new double[binCount];
        var weightedAngles = new double[binCount];
        var usableEdges = 0;
        for (var y = step; y < height - step; y += step)
        {
            for (var x = step; x < width - step; x += step)
            {
                var topLeft = Luminance(bgra32, width, x - step, y - step);
                var top = Luminance(bgra32, width, x, y - step);
                var topRight = Luminance(bgra32, width, x + step, y - step);
                var left = Luminance(bgra32, width, x - step, y);
                var right = Luminance(bgra32, width, x + step, y);
                var bottomLeft = Luminance(bgra32, width, x - step, y + step);
                var bottom = Luminance(bgra32, width, x, y + step);
                var bottomRight = Luminance(bgra32, width, x + step, y + step);
                var gradientX = -topLeft + topRight - 2 * left + 2 * right - bottomLeft + bottomRight;
                var gradientY = -topLeft - 2 * top - topRight + bottomLeft + 2 * bottom + bottomRight;
                var magnitude = Math.Sqrt(gradientX * gradientX + gradientY * gradientY);
                if (magnitude < 48) continue;

                var edgeAngle = Math.Atan2(gradientY, gradientX) * 180.0 / Math.PI + 90.0;
                while (edgeAngle > 90) edgeAngle -= 180;
                while (edgeAngle <= -90) edgeAngle += 180;
                if (Math.Abs(edgeAngle) > MaximumAbsoluteAngle) continue;

                var bin = Math.Clamp((int)Math.Round((edgeAngle + MaximumAbsoluteAngle) / BinSize), 0, binCount - 1);
                weights[bin] += magnitude;
                weightedAngles[bin] += magnitude * edgeAngle;
                usableEdges++;
            }
        }
        if (usableEdges < 24) return false;

        var peak = Array.IndexOf(weights, weights.Max());
        if (weights[peak] <= 0) return false;
        var totalWeight = 0.0;
        var totalAngle = 0.0;
        for (var bin = Math.Max(0, peak - 1); bin <= Math.Min(weights.Length - 1, peak + 1); bin++)
        {
            totalWeight += weights[bin];
            totalAngle += weightedAngles[bin];
        }
        if (totalWeight <= 0) return false;
        correctionDegrees = Math.Clamp(-(totalAngle / totalWeight), -MaximumAbsoluteAngle, MaximumAbsoluteAngle);
        return true;
    }

    private static double Luminance(byte[] pixels, int width, int x, int y)
    {
        var index = (y * width + x) * 4;
        return pixels[index] * 0.0722 + pixels[index + 1] * 0.7152 + pixels[index + 2] * 0.2126;
    }
}
