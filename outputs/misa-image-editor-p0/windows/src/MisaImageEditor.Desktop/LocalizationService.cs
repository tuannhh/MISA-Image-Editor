using System.ComponentModel;
using System.IO;
using System.Text.Json;

namespace MisaImageEditor.Desktop;

/// <summary>
/// Lightweight application-local i18n service. The indexer is bindable from WPF
/// and publishes Item[] changes when the selected language changes.
/// </summary>
public sealed class LocalizationService : INotifyPropertyChanged
{
    private const string Vietnamese = "vi";
    private const string English = "en";
    private string _language = Vietnamese;
    private string? _settingsPath;

    private static readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> Strings =
        new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.OrdinalIgnoreCase)
        {
            [Vietnamese] = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["NoTarget"] = "Chưa chọn collection đích",
                ["Selected"] = "đã chọn",
                ["Scanning"] = "Đang đọc danh sách ảnh…",
                ["ImportFinished"] = "Đã nhập {0} ảnh trong {1} giây. Ảnh thu nhỏ đang được tải.",
                ["ImportFailed"] = "Không nhập được ảnh: {0}",
                ["PreviewFailed"] = "Không xem được ảnh: {0}",
                ["SelectPhotos"] = "Hãy chọn ảnh trước.",
                ["AddedToTarget"] = "Đã thêm {0}/{1} ảnh vào {2}.",
                ["AddToTarget"] = "Thêm vào collection  ·  B",
                ["SetTargetExisting"] = "Đặt làm collection đích",
                ["WindowTitle"] = "MISA Image Editor - P0.2",
                ["AppProduct"] = "MISA Image Editor",
                ["NavLibrary"] = "Thư viện",
                ["NavEditor"] = "Chỉnh sửa",
                ["Language"] = "Ngôn ngữ",
                ["Theme"] = "Giao diện",
                ["ThemeSystem"] = "Theo hệ thống",
                ["ThemeLight"] = "Sáng",
                ["ThemeDark"] = "Tối",
                ["ThemeChanged"] = "Đã đổi giao diện.",
                ["PrototypeInfo"] = "P0 thử nghiệm · Ứng dụng Windows · catalog cục bộ",
                ["ReadyImport"] = "Sẵn sàng — hãy import ảnh",
                ["Photos"] = "ảnh",
                ["ImportFolder"] = "Import thư mục",
                ["CreateCollection"] = "Tạo collection",
                ["ShowLastImport"] = "Xem lần import gần nhất",
                ["ShowAllCatalog"] = "Xem toàn bộ catalog",
                ["Collections"] = "Collections",
                ["Target"] = "target",
                ["TargetShortcut"] = "Phím tắt: B thêm ảnh đã chọn vào target collection",
                ["LastImport"] = "Lần import gần nhất",
                ["NoImportSession"] = "Chưa có lần import",
                ["ImportPhotosPreview"] = "Import ảnh để xem trước",
                ["EditBasic"] = "Chỉnh sửa — Cơ bản",
                ["CopySettings"] = "Copy thiết lập...",
                ["PasteSelected"] = "Paste vào ảnh đã chọn",
                ["SavePreset"] = "Lưu preset...",
                ["ApplyPreset"] = "Áp dụng preset...",
                ["Exposure"] = "Phơi sáng",
                ["Contrast"] = "Tương phản",
                ["Highlights"] = "Vùng sáng",
                ["Shadows"] = "Vùng tối",
                ["Whites"] = "Trắng",
                ["Blacks"] = "Đen",
                ["Saturation"] = "Độ bão hòa",
                ["Export"] = "Xuất ảnh",
                ["ExportCurrent"] = "Xuất ảnh hiện tại...",
                ["ExportSelected"] = "Xuất ảnh đã chọn...",
                ["BatchSubfolder"] = "Thư mục con batch (tùy chọn)",
                ["BatchFormat"] = "Định dạng batch",
                ["OutputWidth"] = "Chiều rộng xuất (0 = gốc)",
                ["OutputHeight"] = "Chiều cao xuất (0 = gốc)",
                ["WatermarkText"] = "Nội dung watermark",
                ["WatermarkOpacity"] = "Độ mờ watermark",
                ["WatermarkPosition"] = "Vị trí watermark",
                ["PositionTopLeft"] = "Trên trái",
                ["PositionTopRight"] = "Trên phải",
                ["PositionCenter"] = "Chính giữa",
                ["PositionBottomLeft"] = "Dưới trái",
                ["PositionBottomRight"] = "Dưới phải",
                ["EditSections"] = "Các mục chỉnh sửa",
                ["ToneCurve"] = "Đường cong tông màu",
                ["ToneCurveDescription"] = "Đường cong tông màu tham số bốn vùng",
                ["Darks"] = "Vùng tối sâu",
                ["Lights"] = "Vùng sáng giữa",
                ["ColorMixer"] = "Trộn màu",
                ["ColorMixerDescription"] = "Chỉnh HSL theo dải màu",
                ["Hue"] = "Sắc độ",
                ["Luminance"] = "Độ sáng",
                ["ColorRed"] = "Đỏ",
                ["ColorOrange"] = "Cam",
                ["ColorYellow"] = "Vàng",
                ["ColorGreen"] = "Lục",
                ["ColorAqua"] = "Xanh ngọc",
                ["ColorBlue"] = "Lam",
                ["ColorPurple"] = "Tím",
                ["ColorMagenta"] = "Hồng tím",
                ["LensCorrections"] = "Hiệu chỉnh ống kính",
                ["LensDescription"] = "Vignetting thủ công và metadata profile",
                ["EnableProfile"] = "Bật metadata profile đã kiểm chứng",
                ["ManualDistortion"] = "Méo thủ công (áp dụng khi xem trước/xuất)",
                ["ManualVignetting"] = "Vignetting thủ công (-100 đến 100)",
                ["LensStatusDefault"] = "Profile được ghi nhận rõ ràng; vignetting thủ công được áp dụng.",
                ["TransformCrop"] = "Biến đổi / Crop",
                ["CropDescription"] = "Crop và xoay preview JPEG/PNG",
                ["AspectRatio"] = "Tỷ lệ khung hình",
                ["AspectOriginal"] = "Gốc",
                ["Angle"] = "Góc (-45 đến 45 độ)",
                ["AutoStraighten"] = "Tự căn đường chân trời",
                ["ResetCrop"] = "Đặt lại crop",
                ["AutoStraightenHint"] = "Tự căn ước lượng cạnh gần nằm ngang mạnh nhất; hãy xem lại trước khi xuất.",
                ["Mask"] = "Mask",
                ["MaskDescription"] = "Điều chỉnh cục bộ bằng raster mask (P0)",
                ["ChooseMask"] = "Chọn ảnh mask...",
                ["PaintMask"] = "Vẽ mask bằng cọ...",
                ["AutoSubjectMask"] = "Tự động mask chủ thể...",
                ["ClearMask"] = "Xóa mask",
                ["NoMaskSelected"] = "Chưa chọn mask",
                ["InvertMask"] = "Đảo mask (hậu cảnh)",
                ["MaskExposure"] = "Phơi sáng mask",
                ["MaskHint"] = "Có cọ và mask chủ thể offline; undo, lực bút và tinh chỉnh sẽ làm ở giai đoạn sau.",
                ["CollectionDialogTitle"] = "Tạo collection",
                ["CollectionName"] = "Tên collection",
                ["IncludeSelected"] = "Thêm các ảnh đang chọn",
                ["SetTarget"] = "Đặt làm target collection",
                ["Create"] = "Tạo",
                ["Cancel"] = "Hủy",
                ["MissingName"] = "Thiếu tên",
                ["MissingNameMessage"] = "Hãy nhập tên collection.",
                ["CopySettingsTitle"] = "Copy thiết lập",
                ["CopyInstruction"] = "Chọn các thiết lập sẽ copy sang ảnh đã chọn.",
                ["BasicAdjustments"] = "Chỉnh cơ bản (phơi sáng, tương phản, vùng sáng/tối, trắng/đen, bão hòa)",
                ["CropTransform"] = "Crop và biến đổi",
                ["MaskLocal"] = "Mask và điều chỉnh cục bộ",
                ["ExportWatermark"] = "Watermark và kích thước xuất",
                ["CheckAll"] = "Chọn tất cả",
                ["CheckNone"] = "Bỏ chọn tất cả",
                ["Copy"] = "Copy",
                ["CopyAtLeastOne"] = "Hãy chọn ít nhất một nhóm thiết lập.",
                ["PaintMaskTitle"] = "Vẽ Mask",
                ["PaintMaskInstruction"] = "Tô trắng vào mask. Màu đen là vùng không mask; tọa độ mask theo ảnh gốc.",
                ["BrushSize"] = "Kích thước cọ",
                ["Hardness"] = "Độ cứng",
                ["Clear"] = "Xóa",
                ["SaveMask"] = "Lưu mask",
                ["LanguageChanged"] = "Đã chuyển giao diện sang Tiếng Việt.",
            },
            [English] = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["NoTarget"] = "No target collection",
                ["Selected"] = "selected",
                ["Scanning"] = "Reading photo list…",
                ["ImportFinished"] = "Imported {0} photos in {1} seconds. Thumbnails are loading.",
                ["ImportFailed"] = "Import failed: {0}",
                ["PreviewFailed"] = "Preview failed: {0}",
                ["SelectPhotos"] = "Select photos first.",
                ["AddedToTarget"] = "Added {0}/{1} photos to {2}.",
                ["AddToTarget"] = "Add to collection  ·  B",
                ["SetTargetExisting"] = "Set as target collection",
                ["WindowTitle"] = "MISA Image Editor - P0",
                ["AppProduct"] = "MISA Image Editor",
                ["NavLibrary"] = "Library",
                ["NavEditor"] = "Editor",
                ["Language"] = "Language",
                ["Theme"] = "Appearance",
                ["ThemeSystem"] = "System setting",
                ["ThemeLight"] = "Light",
                ["ThemeDark"] = "Dark",
                ["ThemeChanged"] = "Appearance changed.",
                ["PrototypeInfo"] = "P0 prototype · Windows desktop · local catalog",
                ["ReadyImport"] = "Ready — import photos",
                ["Photos"] = "photos",
                ["ImportFolder"] = "Import folder",
                ["CreateCollection"] = "Create collection",
                ["ShowLastImport"] = "Show last import",
                ["ShowAllCatalog"] = "Show all catalog",
                ["Collections"] = "Collections",
                ["Target"] = "target",
                ["TargetShortcut"] = "Shortcut: B adds selected photos to the target collection",
                ["LastImport"] = "Last import",
                ["NoImportSession"] = "No import session",
                ["ImportPhotosPreview"] = "Import photos to preview",
                ["EditBasic"] = "Edit — Basic",
                ["CopySettings"] = "Copy settings...",
                ["PasteSelected"] = "Paste to selected",
                ["SavePreset"] = "Save preset...",
                ["ApplyPreset"] = "Apply preset...",
                ["Exposure"] = "Exposure",
                ["Contrast"] = "Contrast",
                ["Highlights"] = "Highlights",
                ["Shadows"] = "Shadows",
                ["Whites"] = "Whites",
                ["Blacks"] = "Blacks",
                ["Saturation"] = "Saturation",
                ["Export"] = "Export",
                ["ExportCurrent"] = "Export current...",
                ["ExportSelected"] = "Export selected...",
                ["BatchSubfolder"] = "Batch subfolder (optional)",
                ["BatchFormat"] = "Batch format",
                ["OutputWidth"] = "Output width (0 = original)",
                ["OutputHeight"] = "Output height (0 = original)",
                ["WatermarkText"] = "Watermark text",
                ["WatermarkOpacity"] = "Watermark opacity",
                ["WatermarkPosition"] = "Watermark position",
                ["PositionTopLeft"] = "Top left",
                ["PositionTopRight"] = "Top right",
                ["PositionCenter"] = "Center",
                ["PositionBottomLeft"] = "Bottom left",
                ["PositionBottomRight"] = "Bottom right",
                ["EditSections"] = "Edit sections",
                ["ToneCurve"] = "Tone Curve",
                ["ToneCurveDescription"] = "Four-region parametric tone curve",
                ["Darks"] = "Darks",
                ["Lights"] = "Lights",
                ["ColorMixer"] = "Color Mixer",
                ["ColorMixerDescription"] = "HSL adjustments by color range",
                ["Hue"] = "Hue",
                ["Luminance"] = "Luminance",
                ["ColorRed"] = "Red",
                ["ColorOrange"] = "Orange",
                ["ColorYellow"] = "Yellow",
                ["ColorGreen"] = "Green",
                ["ColorAqua"] = "Aqua",
                ["ColorBlue"] = "Blue",
                ["ColorPurple"] = "Purple",
                ["ColorMagenta"] = "Magenta",
                ["LensCorrections"] = "Lens Corrections",
                ["LensDescription"] = "Manual vignetting plus profile metadata",
                ["EnableProfile"] = "Enable verified profile metadata",
                ["ManualDistortion"] = "Manual distortion (applied to preview/export)",
                ["ManualVignetting"] = "Manual vignetting (-100 to 100)",
                ["LensStatusDefault"] = "Profile calibration is reported explicitly; manual vignetting is applied.",
                ["TransformCrop"] = "Transform / Crop",
                ["CropDescription"] = "Crop and rotate JPEG/PNG preview",
                ["AspectRatio"] = "Aspect ratio",
                ["AspectOriginal"] = "Original",
                ["Angle"] = "Angle (-45 to 45 degrees)",
                ["AutoStraighten"] = "Auto straighten",
                ["ResetCrop"] = "Reset crop",
                ["AutoStraightenHint"] = "Auto straighten estimates the strongest near-horizontal edges; review the result before export.",
                ["Mask"] = "Mask",
                ["MaskDescription"] = "Raster mask local adjustment (P0)",
                ["ChooseMask"] = "Choose mask image...",
                ["PaintMask"] = "Paint mask with brush...",
                ["AutoSubjectMask"] = "Auto subject mask...",
                ["ClearMask"] = "Clear mask",
                ["NoMaskSelected"] = "No mask selected",
                ["InvertMask"] = "Invert mask (background)",
                ["MaskExposure"] = "Mask exposure",
                ["MaskHint"] = "Brush and offline subject mask are available; undo, pressure, and refinement remain later scope.",
                ["CollectionDialogTitle"] = "Create collection",
                ["CollectionName"] = "Collection name",
                ["IncludeSelected"] = "Include selected photos",
                ["SetTarget"] = "Set as target collection",
                ["Create"] = "Create",
                ["Cancel"] = "Cancel",
                ["MissingName"] = "Missing name",
                ["MissingNameMessage"] = "Enter a collection name.",
                ["CopySettingsTitle"] = "Copy Settings",
                ["CopyInstruction"] = "Choose settings to copy to the selected photos.",
                ["BasicAdjustments"] = "Basic adjustments (exposure, contrast, highlights, shadows, whites, blacks, saturation)",
                ["CropTransform"] = "Crop and transform",
                ["MaskLocal"] = "Mask and local adjustments",
                ["ExportWatermark"] = "Export watermark and resize settings",
                ["CheckAll"] = "Check all",
                ["CheckNone"] = "Check none",
                ["Copy"] = "Copy",
                ["CopyAtLeastOne"] = "Select at least one settings group.",
                ["PaintMaskTitle"] = "Paint Mask",
                ["PaintMaskInstruction"] = "Paint white into the mask. Black remains unmasked; mask coordinates follow the original image.",
                ["BrushSize"] = "Brush size",
                ["Hardness"] = "Hardness",
                ["Clear"] = "Clear",
                ["SaveMask"] = "Save mask",
                ["LanguageChanged"] = "Interface switched to English.",
            }
        };

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Language => _language;

    public string this[string key] => Get(key);

    public void Initialize(string applicationDataDirectory)
    {
        _settingsPath = Path.Combine(applicationDataDirectory, "language.json");
        try
        {
            if (!File.Exists(_settingsPath)) return;
            var saved = JsonSerializer.Deserialize<LanguageSettings>(File.ReadAllText(_settingsPath));
            if (saved?.Language is English or Vietnamese) _language = saved.Language;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
        }
        catch (IOException) { }
        catch (JsonException) { }
    }

    public void SetLanguage(string? language)
    {
        var normalized = string.Equals(language, English, StringComparison.OrdinalIgnoreCase) ? English : Vietnamese;
        if (string.Equals(_language, normalized, StringComparison.Ordinal)) return;
        _language = normalized;
        try
        {
            if (!string.IsNullOrWhiteSpace(_settingsPath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_settingsPath)!);
                File.WriteAllText(_settingsPath, JsonSerializer.Serialize(new LanguageSettings(_language)));
            }
        }
        catch (IOException) { }
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Language)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
    }

    public string Get(string key) =>
        Strings.TryGetValue(_language, out var current) && current.TryGetValue(key, out var value)
            ? value
            : Strings[English].TryGetValue(key, out var fallback) ? fallback : key;

    private sealed record LanguageSettings(string Language);
}
