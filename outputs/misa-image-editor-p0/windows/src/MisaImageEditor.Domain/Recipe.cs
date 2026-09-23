using System.Text.Json.Serialization;

namespace MisaImageEditor.Domain;

public sealed record BasicAdjustments(
    [property: JsonPropertyName("exposure")] double Exposure = 0,
    [property: JsonPropertyName("contrast")] double Contrast = 0,
    [property: JsonPropertyName("highlights")] double Highlights = 0,
    [property: JsonPropertyName("shadows")] double Shadows = 0,
    [property: JsonPropertyName("whites")] double Whites = 0,
    [property: JsonPropertyName("blacks")] double Blacks = 0,
    [property: JsonPropertyName("saturation")] double Saturation = 0);

public sealed record ToneCurveAdjustments(
    [property: JsonPropertyName("shadows")] double Shadows = 0,
    [property: JsonPropertyName("darks")] double Darks = 0,
    [property: JsonPropertyName("lights")] double Lights = 0,
    [property: JsonPropertyName("highlights")] double Highlights = 0);

public sealed record ColorMixerChannel(
    [property: JsonPropertyName("hue")] double Hue = 0,
    [property: JsonPropertyName("saturation")] double Saturation = 0,
    [property: JsonPropertyName("luminance")] double Luminance = 0);

public sealed record ColorMixerAdjustments(
    [property: JsonPropertyName("channels")] IReadOnlyDictionary<string, ColorMixerChannel> Channels = null!)
{
    public static readonly string[] ChannelNames = { "red", "orange", "yellow", "green", "aqua", "blue", "purple", "magenta" };

    public static ColorMixerAdjustments Default => new(
        ChannelNames.ToDictionary(name => name, _ => new ColorMixerChannel(), StringComparer.OrdinalIgnoreCase));

    public ColorMixerChannel Get(string name) =>
        Channels is not null && Channels.TryGetValue(name, out var channel) ? channel : new ColorMixerChannel();
}

public sealed record LensCorrectionSettings(
    [property: JsonPropertyName("profile_id")] string? ProfileId = null,
    [property: JsonPropertyName("profile_enabled")] bool ProfileEnabled = false,
    [property: JsonPropertyName("distortion")] double Distortion = 0,
    [property: JsonPropertyName("vignetting")] double Vignetting = 0);

public sealed record CropRect(
    [property: JsonPropertyName("left")] double Left = 0,
    [property: JsonPropertyName("top")] double Top = 0,
    [property: JsonPropertyName("right")] double Right = 1,
    [property: JsonPropertyName("bottom")] double Bottom = 1,
    [property: JsonPropertyName("angle")] double Angle = 0,
    [property: JsonPropertyName("aspect")] string Aspect = "original");

public sealed record WatermarkSettings(
    [property: JsonPropertyName("text")] string? Text = null,
    [property: JsonPropertyName("opacity")] double Opacity = 65,
    [property: JsonPropertyName("margin")] int Margin = 24,
    [property: JsonPropertyName("position")] string Position = "bottom_right",
    [property: JsonPropertyName("width")] int? Width = null,
    [property: JsonPropertyName("height")] int? Height = null);

public sealed record MaskReference(
    [property: JsonPropertyName("type")] string Type = "rectangle",
    [property: JsonPropertyName("path")] string? Path = null,
    [property: JsonPropertyName("left")] double Left = 0,
    [property: JsonPropertyName("top")] double Top = 0,
    [property: JsonPropertyName("right")] double Right = 1,
    [property: JsonPropertyName("bottom")] double Bottom = 1,
    [property: JsonPropertyName("basic")] BasicAdjustments? Basic = null,
    [property: JsonPropertyName("invert")] bool Invert = false);

public sealed record EditRecipe(
    [property: JsonPropertyName("schema_version")] int SchemaVersion = 1,
    [property: JsonPropertyName("process_version")] string ProcessVersion = "p0-rgb-1",
    [property: JsonPropertyName("basic")] BasicAdjustments Basic = null!,
    [property: JsonPropertyName("crop")] CropRect Crop = null!,
    [property: JsonPropertyName("watermark")] WatermarkSettings? Watermark = null,
    [property: JsonPropertyName("mask")] MaskReference? Mask = null,
    [property: JsonPropertyName("tone_curve")] ToneCurveAdjustments ToneCurve = null!,
    [property: JsonPropertyName("color_mixer")] ColorMixerAdjustments ColorMixer = null!,
    [property: JsonPropertyName("lens_correction")] LensCorrectionSettings LensCorrection = null!)
{
    public static EditRecipe Default => new(1, "p0-rgb-1", new BasicAdjustments(), new CropRect(), null, null, new ToneCurveAdjustments(), ColorMixerAdjustments.Default, new LensCorrectionSettings());
}

public sealed record RecipeCopy(
    int SourceAssetId,
    IReadOnlySet<string> SelectedFields,
    EditRecipe Snapshot)
{
    public EditRecipe ApplyTo(EditRecipe destination)
    {
        var basic = destination.Basic;
        var crop = destination.Crop;
        var watermark = destination.Watermark;
        var mask = destination.Mask;
        var toneCurve = destination.ToneCurve ?? new ToneCurveAdjustments();
        var colorMixer = destination.ColorMixer ?? ColorMixerAdjustments.Default;
        var lensCorrection = destination.LensCorrection ?? new LensCorrectionSettings();

        if (SelectedFields.Contains("basic.exposure")) basic = basic with { Exposure = Snapshot.Basic.Exposure };
        if (SelectedFields.Contains("basic.contrast")) basic = basic with { Contrast = Snapshot.Basic.Contrast };
        if (SelectedFields.Contains("basic.highlights")) basic = basic with { Highlights = Snapshot.Basic.Highlights };
        if (SelectedFields.Contains("basic.shadows")) basic = basic with { Shadows = Snapshot.Basic.Shadows };
        if (SelectedFields.Contains("basic.whites")) basic = basic with { Whites = Snapshot.Basic.Whites };
        if (SelectedFields.Contains("basic.blacks")) basic = basic with { Blacks = Snapshot.Basic.Blacks };
        if (SelectedFields.Contains("basic.saturation")) basic = basic with { Saturation = Snapshot.Basic.Saturation };
        if (SelectedFields.Contains("crop")) crop = Snapshot.Crop;
        if (SelectedFields.Contains("watermark")) watermark = Snapshot.Watermark;
        if (SelectedFields.Contains("mask")) mask = Snapshot.Mask;
        if (SelectedFields.Contains("tone_curve")) toneCurve = Snapshot.ToneCurve ?? new ToneCurveAdjustments();
        if (SelectedFields.Contains("color_mixer")) colorMixer = Snapshot.ColorMixer ?? ColorMixerAdjustments.Default;
        if (SelectedFields.Contains("lens_correction")) lensCorrection = Snapshot.LensCorrection ?? new LensCorrectionSettings();

        return destination with { Basic = basic, Crop = crop, Watermark = watermark, Mask = mask, ToneCurve = toneCurve, ColorMixer = colorMixer, LensCorrection = lensCorrection };
    }
}
