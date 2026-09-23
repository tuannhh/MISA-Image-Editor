using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using WpfMouseEventArgs = System.Windows.Input.MouseEventArgs;
using WpfPoint = System.Windows.Point;

namespace MisaImageEditor.Desktop;

public partial class MaskBrushDialog : Window
{
    private readonly LocalizationService _i18n;
    private readonly int _pixelWidth;
    private readonly int _pixelHeight;
    private readonly byte[] _maskPixels;
    private bool _painting;
    private WpfPoint _lastPoint;

    public MaskBrushDialog(LocalizationService i18n, BitmapSource source)
    {
        _i18n = i18n;
        InitializeComponent();
        Title = _i18n["PaintMaskTitle"];
        InstructionText.Text = _i18n["PaintMaskInstruction"];
        BrushSizeLabel.Text = _i18n["BrushSize"];
        HardnessLabel.Text = _i18n["Hardness"];
        ClearButton.Content = _i18n["Clear"];
        SaveButton.Content = _i18n["SaveMask"];
        CancelButton.Content = _i18n["Cancel"];
        _pixelWidth = Math.Max(1, source.PixelWidth);
        _pixelHeight = Math.Max(1, source.PixelHeight);
        _maskPixels = new byte[_pixelWidth * _pixelHeight];
        SourceImage.Source = source;
        UpdateSettingLabels();
    }

    public string? MaskPath { get; private set; }

    private void BrushSettingChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        UpdateSettingLabels();
    }

    private void UpdateSettingLabels()
    {
        if (BrushSizeValue is null) return;
        BrushSizeValue.Text = $"{BrushSizeSlider.Value:0} px";
    }

    private void BrushCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _painting = true;
        _lastPoint = e.GetPosition(BrushCanvas);
        BrushCanvas.CaptureMouse();
        PaintSegment(_lastPoint, _lastPoint);
        e.Handled = true;
    }

    private void BrushCanvas_MouseMove(object sender, WpfMouseEventArgs e)
    {
        if (!_painting || e.LeftButton != MouseButtonState.Pressed) return;
        var point = e.GetPosition(BrushCanvas);
        PaintSegment(_lastPoint, point);
        _lastPoint = point;
        e.Handled = true;
    }

    private void BrushCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        _painting = false;
        BrushCanvas.ReleaseMouseCapture();
        e.Handled = true;
    }

    private void PaintSegment(WpfPoint start, WpfPoint end)
    {
        var displayWidth = Math.Max(1.0, BrushCanvas.ActualWidth);
        var displayHeight = Math.Max(1.0, BrushCanvas.ActualHeight);
        var distance = (end - start).Length;
        var steps = Math.Max(1, (int)Math.Ceiling(distance / Math.Max(1.0, BrushSizeSlider.Value / 4.0)));
        for (var step = 0; step <= steps; step++)
        {
            var t = steps == 0 ? 0 : step / (double)steps;
            var point = new WpfPoint(start.X + (end.X - start.X) * t, start.Y + (end.Y - start.Y) * t);
            PaintDab(point, displayWidth, displayHeight);
        }
    }

    private void PaintDab(WpfPoint point, double displayWidth, double displayHeight)
    {
        var centerX = (int)Math.Round(Math.Clamp(point.X / displayWidth, 0, 1) * (_pixelWidth - 1));
        var centerY = (int)Math.Round(Math.Clamp(point.Y / displayHeight, 0, 1) * (_pixelHeight - 1));
        var radiusX = Math.Max(1.0, BrushSizeSlider.Value / displayWidth * _pixelWidth / 2.0);
        var radiusY = Math.Max(1.0, BrushSizeSlider.Value / displayHeight * _pixelHeight / 2.0);
        var minX = Math.Max(0, (int)Math.Floor(centerX - radiusX));
        var maxX = Math.Min(_pixelWidth - 1, (int)Math.Ceiling(centerX + radiusX));
        var minY = Math.Max(0, (int)Math.Floor(centerY - radiusY));
        var maxY = Math.Min(_pixelHeight - 1, (int)Math.Ceiling(centerY + radiusY));
        var hardness = Math.Clamp(BrushHardnessSlider.Value / 100.0, 0, 1);
        for (var y = minY; y <= maxY; y++)
        {
            for (var x = minX; x <= maxX; x++)
            {
                var dx = (x - centerX) / radiusX;
                var dy = (y - centerY) / radiusY;
                var distance = Math.Sqrt(dx * dx + dy * dy);
                if (distance > 1) continue;
                var alpha = distance <= hardness || hardness >= 0.999
                    ? 1.0
                    : Math.Clamp((1.0 - distance) / Math.Max(0.001, 1.0 - hardness), 0, 1);
                var index = y * _pixelWidth + x;
                _maskPixels[index] = Math.Max(_maskPixels[index], (byte)Math.Round(alpha * 255));
            }
        }
        var ellipse = new Ellipse
        {
            Width = BrushSizeSlider.Value,
            Height = BrushSizeSlider.Value,
            Stroke = new SolidColorBrush(System.Windows.Media.Color.FromArgb(180, 255, 255, 255)),
            Fill = new SolidColorBrush(System.Windows.Media.Color.FromArgb((byte)Math.Clamp(35 + BrushHardnessSlider.Value, 35, 135), 255, 255, 255)),
            IsHitTestVisible = false
        };
        Canvas.SetLeft(ellipse, point.X - BrushSizeSlider.Value / 2.0);
        Canvas.SetTop(ellipse, point.Y - BrushSizeSlider.Value / 2.0);
        BrushCanvas.Children.Add(ellipse);
    }

    private void Clear_Click(object sender, RoutedEventArgs e)
    {
        Array.Clear(_maskPixels, 0, _maskPixels.Length);
        BrushCanvas.Children.Clear();
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        var directory = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MisaImageEditor", "masks");
        Directory.CreateDirectory(directory);
        var path = System.IO.Path.Combine(directory, $"brush-mask-{Guid.NewGuid():N}.png");
        var mask = new WriteableBitmap(_pixelWidth, _pixelHeight, 96, 96, PixelFormats.Gray8, null);
        mask.WritePixels(new Int32Rect(0, 0, _pixelWidth, _pixelHeight), _maskPixels, _pixelWidth, 0);
        mask.Freeze();
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(mask));
        using (var stream = File.Create(path)) encoder.Save(stream);
        MaskPath = path;
        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
