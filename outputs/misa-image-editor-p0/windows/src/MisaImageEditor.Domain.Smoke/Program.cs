using MisaImageEditor.Domain;
using System.Text.Json;

var root = Path.Combine(Path.GetTempPath(), "misa-domain-smoke-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(root);
try
{
    var catalogPath = Path.Combine(root, "catalog.json");
    var imagePath = Path.Combine(root, "photo.jpg");
    var serializedRecipe = JsonSerializer.Serialize(EditRecipe.Default with
    {
        Crop = new CropRect(0.1, 0.05, 0.9, 0.95, 2.5, "3:2"),
        ToneCurve = new ToneCurveAdjustments(Shadows: 12, Darks: -8, Lights: 6, Highlights: -15),
        ColorMixer = new ColorMixerAdjustments(new Dictionary<string, ColorMixerChannel>(StringComparer.OrdinalIgnoreCase)
        {
            ["red"] = new ColorMixerChannel(Hue: 8, Saturation: 12, Luminance: -4)
        }),
        LensCorrection = new LensCorrectionSettings("manual", true, -12, 18),
        Watermark = new WatermarkSettings("MISA", 70, 24, "center", 1200, null),
        Mask = new MaskReference("raster", "mask.png", Invert: true)
    });
    if (!serializedRecipe.Contains("\"schema_version\"", StringComparison.Ordinal) ||
        serializedRecipe.Contains("\"schemaVersion\"", StringComparison.Ordinal))
        throw new InvalidOperationException("recipe JSON names do not match recipe-v1 contract");
    if (!serializedRecipe.Contains("\"mask\"", StringComparison.Ordinal) ||
        !serializedRecipe.Contains("\"path\":\"mask.png\"", StringComparison.Ordinal) ||
        !serializedRecipe.Contains("\"invert\":true", StringComparison.Ordinal))
        throw new InvalidOperationException("raster mask JSON does not match recipe-v1 contract");
    if (!serializedRecipe.Contains("\"position\":\"center\"", StringComparison.Ordinal) ||
        !serializedRecipe.Contains("\"width\":1200", StringComparison.Ordinal))
        throw new InvalidOperationException("watermark JSON does not match recipe-v1 contract");
    if (!serializedRecipe.Contains("\"angle\":2.5", StringComparison.Ordinal) ||
        !serializedRecipe.Contains("\"aspect\":\"3:2\"", StringComparison.Ordinal))
        throw new InvalidOperationException("crop geometry JSON does not match recipe-v1 contract");
    if (!serializedRecipe.Contains("\"tone_curve\"", StringComparison.Ordinal) ||
        !serializedRecipe.Contains("\"highlights\":-15", StringComparison.Ordinal))
        throw new InvalidOperationException("tone curve JSON does not match recipe-v1 contract");
    if (!serializedRecipe.Contains("\"color_mixer\"", StringComparison.Ordinal) ||
        !serializedRecipe.Contains("\"red\"", StringComparison.Ordinal) ||
        !serializedRecipe.Contains("\"saturation\":12", StringComparison.Ordinal))
        throw new InvalidOperationException("color mixer JSON does not match recipe-v1 contract");
    if (!serializedRecipe.Contains("\"lens_correction\"", StringComparison.Ordinal) ||
        !serializedRecipe.Contains("\"vignetting\":18", StringComparison.Ordinal))
        throw new InvalidOperationException("lens correction JSON does not match recipe-v1 contract");
    var copySnapshot = EditRecipe.Default with
    {
        Crop = new CropRect(0.1, 0.05, 0.9, 0.95, 2.5, "3:2"),
        ToneCurve = new ToneCurveAdjustments(Shadows: 10, Lights: -4),
        ColorMixer = new ColorMixerAdjustments(new Dictionary<string, ColorMixerChannel>(StringComparer.OrdinalIgnoreCase)
        {
            ["blue"] = new ColorMixerChannel(Hue: -5, Saturation: 20, Luminance: 7)
        }),
        LensCorrection = new LensCorrectionSettings("manual", true, 5, -9),
        Basic = new BasicAdjustments(Exposure: 1.5),
        Mask = new MaskReference("raster", "copy-mask.png", Invert: true),
        Watermark = new WatermarkSettings("Copied", 55, 16, "top_left", 800, null)
    };
    var copy = new RecipeCopy(1, new HashSet<string> { "basic.exposure", "mask", "watermark", "crop", "tone_curve", "color_mixer", "lens_correction" }, copySnapshot);
    var appliedCopy = copy.ApplyTo(EditRecipe.Default with { Basic = new BasicAdjustments(Contrast: 20) });
    if (Math.Abs(appliedCopy.Basic.Exposure - 1.5) > 0.001 || Math.Abs(appliedCopy.Basic.Contrast - 20) > 0.001 ||
        appliedCopy.Mask?.Path != "copy-mask.png" || appliedCopy.Watermark?.Position != "top_left" ||
        appliedCopy.Crop.Aspect != "3:2" || Math.Abs(appliedCopy.Crop.Angle - 2.5) > 0.001 ||
        Math.Abs((appliedCopy.ToneCurve ?? new ToneCurveAdjustments()).Shadows - 10) > 0.001 ||
        Math.Abs((appliedCopy.ColorMixer ?? ColorMixerAdjustments.Default).Get("blue").Saturation - 20) > 0.001 ||
        Math.Abs((appliedCopy.LensCorrection ?? new LensCorrectionSettings()).Vignetting + 9) > 0.001)
        throw new InvalidOperationException("recipe copy did not preserve selected groups and destination fields");
    var presetStore = new JsonPresetStore(Path.Combine(root, "presets"));
    var presetPath = presetStore.Save(EditPreset.Create("Event look / warm", new[] { "basic.exposure", "color_mixer" }, copySnapshot));
    var reopenedPreset = presetStore.Load(presetPath);
    var presetApplied = reopenedPreset.ToRecipeCopy().ApplyTo(EditRecipe.Default with { Basic = new BasicAdjustments(Contrast: 22) });
    if (reopenedPreset.Name != "Event look / warm" || !reopenedPreset.SelectedFields.Contains("basic.exposure") ||
        Math.Abs(presetApplied.Basic.Exposure - 1.5) > 0.001 || Math.Abs(presetApplied.Basic.Contrast - 22) > 0.001 ||
        Math.Abs((presetApplied.ColorMixer ?? ColorMixerAdjustments.Default).Get("blue").Saturation - 20) > 0.001 ||
        presetApplied.Crop != EditRecipe.Default.Crop)
        throw new InvalidOperationException("preset round-trip did not preserve selected fields only");
    if (presetStore.ListFiles().Count != 1) throw new InvalidOperationException("preset list did not contain saved preset");
    presetStore.Delete(presetPath);
    if (presetStore.ListFiles().Count != 0) throw new InvalidOperationException("preset delete did not remove saved preset");
    var warpSource = new byte[6 * 6 * 4];
    for (var pixel = 0; pixel < warpSource.Length; pixel += 4)
    {
        warpSource[pixel] = (byte)(pixel / 4);
        warpSource[pixel + 1] = (byte)(255 - pixel / 4);
        warpSource[pixel + 2] = (byte)(pixel / 2);
        warpSource[pixel + 3] = 255;
    }
    var warpResult = PixelWarp.ApplyRadialDistortion(warpSource, 6, 6, 65);
    if (warpResult.Length != warpSource.Length || warpResult.Where((value, index) => index % 4 == 3 && value != 255).Any() || warpResult.SequenceEqual(warpSource))
        throw new InvalidOperationException("manual distortion warp did not preserve alpha and change the raster");
    var orientationSource = new byte[2 * 3 * 4];
    for (var pixel = 0; pixel < orientationSource.Length; pixel += 4)
    {
        orientationSource[pixel] = (byte)(pixel / 4);
        orientationSource[pixel + 3] = 255;
    }
    var rotated = PixelOrientation.NormalizeBgra32(orientationSource, 2, 3, 6);
    if (rotated.Width != 3 || rotated.Height != 2 || rotated.Pixels.Length != orientationSource.Length || rotated.Pixels[0] != orientationSource[16])
        throw new InvalidOperationException("EXIF orientation rotation did not produce the expected raster");
    const int horizonWidth = 240;
    const int horizonHeight = 160;
    var horizonSource = new byte[horizonWidth * horizonHeight * 4];
    for (var y = 0; y < horizonHeight; y++)
    {
        for (var x = 0; x < horizonWidth; x++)
        {
            var value = (byte)Math.Clamp(128 + (y - 45 - x * 0.1) * 28, 0, 255);
            var index = (y * horizonWidth + x) * 4;
            horizonSource[index] = horizonSource[index + 1] = horizonSource[index + 2] = value;
            horizonSource[index + 3] = 255;
        }
    }
    if (!HorizonStraightener.TryEstimateCorrectionDegrees(horizonSource, horizonWidth, horizonHeight, out var horizonCorrection) || horizonCorrection is > -3 or < -8.5)
        throw new InvalidOperationException($"horizon correction was not detected: {horizonCorrection}");
    File.WriteAllText(imagePath, "initial");
    var store = new JsonCatalogStore(catalogPath);
    var firstAsset = store.UpsertAsset(new CatalogAsset(imagePath, "Preview ready", ".jpg"));
    if (string.IsNullOrWhiteSpace(firstAsset.Fingerprint) || firstAsset.Fingerprint.Length != 64) throw new InvalidOperationException("fingerprint was not recorded");
    store.CreateCollection("Target", setAsTarget: true);
    store.RecordLastImport(root, new[] { imagePath });
    store.RecordLastImport(root, Array.Empty<string>());
    if (store.LastImportPaths.Count != 1) throw new InvalidOperationException("empty import erased last import");
    store.AddToTarget(new[] { imagePath, imagePath });
    store.SaveRecipe(imagePath, EditRecipe.Default with
    {
        Basic = new BasicAdjustments(Exposure: 1.25),
        ColorMixer = new ColorMixerAdjustments(new Dictionary<string, ColorMixerChannel>(StringComparer.OrdinalIgnoreCase)
        {
            ["red"] = new ColorMixerChannel(Saturation: 14)
        }),
        LensCorrection = new LensCorrectionSettings("manual", true, 0, 11)
    });
    store.UpsertAsset(new CatalogAsset(imagePath, "Preview ready", ".jpg"));
    if (Math.Abs(store.LoadRecipe(imagePath).Basic.Exposure - 1.25) > 0.001) throw new InvalidOperationException("re-import reset recipe");
    if (store.Assets.Single().Fingerprint != firstAsset.Fingerprint) throw new InvalidOperationException("unchanged re-import changed fingerprint");
    File.AppendAllText(imagePath, "changed");
    var changedAsset = store.UpsertAsset(new CatalogAsset(imagePath, "Preview ready", ".jpg"));
    if (changedAsset.Fingerprint == firstAsset.Fingerprint) throw new InvalidOperationException("changed source kept old fingerprint");
    if (Math.Abs(store.LoadRecipe(imagePath).Basic.Exposure - 1.25) > 0.001) throw new InvalidOperationException("source change reset recipe");
    store.Save();

    var reopened = new JsonCatalogStore(catalogPath);
    if (reopened.TargetCollection != "Target") throw new InvalidOperationException("target did not persist");
    if (reopened.LastImportPaths.Count != 1) throw new InvalidOperationException("last import did not persist");
    if (reopened.Collections.Single().AssetPaths.Count != 1) throw new InvalidOperationException("target membership is not idempotent");
    if (Math.Abs(reopened.LoadRecipe(imagePath).Basic.Exposure - 1.25) > 0.001) throw new InvalidOperationException("recipe did not persist");
    if (Math.Abs((reopened.LoadRecipe(imagePath).ColorMixer ?? ColorMixerAdjustments.Default).Get("red").Saturation - 14) > 0.001)
        throw new InvalidOperationException("color mixer did not persist");
    if (Math.Abs((reopened.LoadRecipe(imagePath).LensCorrection ?? new LensCorrectionSettings()).Vignetting - 11) > 0.001)
        throw new InvalidOperationException("lens correction did not persist");
    Console.WriteLine("JsonCatalogStore smoke passed");
}
finally
{
    if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
}
