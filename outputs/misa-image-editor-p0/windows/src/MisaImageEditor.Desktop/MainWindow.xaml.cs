using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MisaImageEditor.Domain;
using Forms = System.Windows.Forms;

namespace MisaImageEditor.Desktop;

public partial class MainWindow : Window
{
    public ObservableCollection<LibraryItem> Files { get; } = new();
    private readonly ObservableCollection<CollectionEntry> _collections = new();
    private readonly ObservableCollection<FolderEntry> _folders = new();
    private readonly Dictionary<string, EditRecipe> _recipes = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _collectionMembership = new(StringComparer.OrdinalIgnoreCase);
    private readonly JsonCatalogStore _catalog;
    private readonly JsonPresetStore _presetStore;
    private readonly LocalizationService _i18n;
    private readonly AppearanceService _appearance;
    private RecipeCopy? _clipboard;
    private int _nextCollection = 1;
    private bool _loadingRecipe;
    private bool _uiReady;
    private BitmapSource? _sourceBitmap;
    private bool _refreshingCollections;
    private readonly System.Threading.SemaphoreSlim _previewSlots = new(2);
    private readonly Dictionary<string, Task<bool>> _previewJobs = new(StringComparer.OrdinalIgnoreCase);
    private System.Threading.CancellationTokenSource? _thumbnailCancellation;
    private readonly System.Windows.Threading.DispatcherTimer _renderTimer = new() { Interval = TimeSpan.FromMilliseconds(100) };
    private int _selectionVersion;
    private int _renderVersion;
    private bool _closed;
    private bool _importing;
    private CatalogView _catalogView = CatalogView.All;
    private string? _activeCollection;
    private string? _activeFolder;
    public Task ImportCompletion { get; private set; } = Task.CompletedTask;
    public Task ThumbnailCompletion { get; private set; } = Task.CompletedTask;

    public MainWindow()
    {
        InitializeComponent();
        var catalogDirectory = Environment.GetEnvironmentVariable("MISA_DATA_DIR") ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MisaImageEditor");
        _i18n = (LocalizationService)FindResource("I18n");
        _i18n.Initialize(catalogDirectory);
        Title = T("WindowTitle");
        _appearance = new AppearanceService();
        _appearance.Initialize(catalogDirectory);
        _appearance.Apply();
        _catalog = new JsonCatalogStore(Path.Combine(catalogDirectory, "catalog.json"));
        _presetStore = new JsonPresetStore(Path.Combine(catalogDirectory, "presets"));
        CollectionsList.ItemsSource = _collections;
        FoldersList.ItemsSource = _folders;
        DataContext = this;
        LoadCatalog();
        SelectLanguage(_i18n.Language);
        SelectTheme(_appearance.Mode);
        _uiReady = true;
        _renderTimer.Tick += (_, _) => { _renderTimer.Stop(); RenderPreviewNow(); };
        SetWorkspace(false);
    }

    private string T(string key) => _i18n[key];

    private bool IsNoMaskText(string? value) =>
        string.Equals(value, T("NoMaskSelected"), StringComparison.Ordinal) ||
        string.Equals(value, "No mask selected", StringComparison.Ordinal) ||
        string.Equals(value, "Chưa chọn mask", StringComparison.Ordinal);

    private void SelectLanguage(string language)
    {
        foreach (var item in LanguageSelector.Items.OfType<System.Windows.Controls.ComboBoxItem>())
        {
            if (string.Equals(item.Tag?.ToString(), language, StringComparison.OrdinalIgnoreCase))
            {
                LanguageSelector.SelectedItem = item;
                return;
            }
        }
        LanguageSelector.SelectedIndex = 0;
    }

    private void LanguageSelector_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (!_uiReady || LanguageSelector.SelectedItem is not System.Windows.Controls.ComboBoxItem item) return;
        _i18n.SetLanguage(item.Tag?.ToString());
        Title = T("WindowTitle");
        RefreshCollections();
        RefreshFolders();
        RefreshLastImportText();
        UpdateCountText();
        if (IsNoMaskText(MaskPathText.Text)) MaskPathText.Text = T("NoMaskSelected");
        StatusText.Text = T("LanguageChanged");
    }

    private void SelectTheme(string mode)
    {
        foreach (var item in ThemeSelector.Items.OfType<System.Windows.Controls.ComboBoxItem>())
        {
            if (string.Equals(item.Tag?.ToString(), mode, StringComparison.OrdinalIgnoreCase))
            {
                ThemeSelector.SelectedItem = item;
                return;
            }
        }
        ThemeSelector.SelectedIndex = 0;
    }

    private void ThemeSelector_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (!_uiReady || ThemeSelector.SelectedItem is not System.Windows.Controls.ComboBoxItem item) return;
        _appearance.SetMode(item.Tag?.ToString());
        StatusText.Text = T("ThemeChanged");
    }

    private void LoadCatalog()
    {
        _recipes.Clear();
        _collectionMembership.Clear();
        foreach (var asset in _catalog.Assets)
        {
            _recipes[asset.Path] = asset.Recipe ?? EditRecipe.Default;
        }
        foreach (var collection in _catalog.Collections)
        {
            foreach (var path in collection.AssetPaths) _collectionMembership[path] = collection.Name;
        }
        PopulateFiles(_catalog.Assets);
        RefreshCollections();
        RefreshLastImportText();
    }

    private void RefreshLastImportText() => LastImportText.Text = _catalog.LastImportPaths.Count == 0
        ? T("NoImportSession")
        : _i18n.Language == "vi"
            ? $"{_catalog.LastImportPaths.Count} ảnh ở lần import gần nhất"
            : $"{_catalog.LastImportPaths.Count} photos in the last import";

    private void UpdateCountText() => CountText.Text = $"{Files.Count} {T("Photos")} · {FilesList.SelectedItems.Count} {T("Selected")}";

    private void PopulateFiles(IEnumerable<CatalogAsset> assets)
    {
        var snapshot = assets.ToArray();
        _thumbnailCancellation?.Cancel();
        _thumbnailCancellation = new();
        _selectionVersion++;
        _renderVersion++;
        _sourceBitmap = null;
        PreviewImage.Source = null;
        EmptyPreviewText.Visibility = Visibility.Visible;
        Files.Clear();
        foreach (var asset in snapshot)
        {
            var cachedPreview = DeferredPreviewPath(asset.Path);
            Files.Add(new LibraryItem(asset.Path, asset.Status, asset.Extension, File.Exists(cachedPreview) ? cachedPreview : null));
        }
        UpdateCountText();
        ThumbnailCompletion = LoadThumbnailsAsync(Files.ToArray(), _thumbnailCancellation.Token);
    }

    private async Task LoadThumbnailsAsync(LibraryItem[] items, System.Threading.CancellationToken token)
    {
        foreach (var item in items.OrderBy(item => item.IsCodecDeferred))
        {
            if (token.IsCancellationRequested || _closed) break;
            try
            {
                if (!item.IsPreviewable && item.IsCodecDeferred)
                    if (!await GenerateDeferredPreviewAsync(item)) continue;
                if (token.IsCancellationRequested || _closed) break;
                var bitmap = await Task.Run(() => LoadDisplayBitmap(item.PreviewSourcePath, 320));
                if (token.IsCancellationRequested || _closed) break;
                item.Thumbnail = bitmap;
            }
            catch (Exception error) when (error is IOException or NotSupportedException or ArgumentException)
            { item.PreviewError = error.Message; }
        }
    }

    private static string DeferredPreviewPath(string sourcePath)
    {
        var fullPath = Path.GetFullPath(sourcePath);
        var info = new FileInfo(fullPath);
        var identity = $"{fullPath}|{(info.Exists ? info.Length : 0)}|{info.LastWriteTimeUtc.Ticks}";
        var key = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(identity))).ToLowerInvariant();
        var directory = Path.Combine(Environment.GetEnvironmentVariable("MISA_DATA_DIR") ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MisaImageEditor"), "previews");
        return Path.Combine(directory, key + ".jpg");
    }

    private static IEnumerable<string> PythonCandidates() => new[]
    {
        Environment.GetEnvironmentVariable("MISA_PYTHON"),
        Environment.GetEnvironmentVariable("MISA_PYTHON", EnvironmentVariableTarget.User),
        "python.exe",
        "python"
    }.Where(candidate => !string.IsNullOrWhiteSpace(candidate)).Select(candidate => candidate!).Distinct(StringComparer.OrdinalIgnoreCase);

    private async Task<bool> GenerateDeferredPreviewAsync(LibraryItem item)
    {
        if (!_previewJobs.TryGetValue(item.FullPath, out var pending))
            _previewJobs[item.FullPath] = pending = GeneratePreviewCoreAsync(item);
        var success = await pending;
        if (success) item.SetPreviewPath(DeferredPreviewPath(item.FullPath));
        else _previewJobs.Remove(item.FullPath);
        return success;
    }

    private async Task<bool> GeneratePreviewCoreAsync(LibraryItem item)
    {
        await _previewSlots.WaitAsync();
        try
        {
        var script = Path.Combine(AppContext.BaseDirectory, "tools", "generate_preview.py");
        if (!File.Exists(script))
        {
            StatusText.Text = "RAW/HEIF preview provider is unavailable: bridge script is missing";
            return false;
        }
        var outputPath = DeferredPreviewPath(item.FullPath);
        if (File.Exists(outputPath))
        {
            item.SetPreviewPath(outputPath);
            return true;
        }
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        StatusText.Text = $"Đang tạo preview codec cho {item.DisplayName}...";
        Exception? lastError = null;
        foreach (var python in PythonCandidates())
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = python!,
                    WorkingDirectory = AppContext.BaseDirectory,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
                startInfo.Environment["PYTHONIOENCODING"] = "utf-8";
                startInfo.ArgumentList.Add(script);
                startInfo.ArgumentList.Add(item.FullPath);
                startInfo.ArgumentList.Add(outputPath);
                using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Could not start Python");
                var stdoutTask = process.StandardOutput.ReadToEndAsync();
                var stderrTask = process.StandardError.ReadToEndAsync();
                using var timeout = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(60));
                try { await process.WaitForExitAsync(timeout.Token); }
                catch (OperationCanceledException) { process.Kill(true); throw new IOException("Preview timed out after 60 seconds"); }
                var stderr = await stderrTask;
                _ = await stdoutTask;
                if (process.ExitCode != 0)
                    throw new InvalidOperationException(string.IsNullOrWhiteSpace(stderr) ? $"provider exit code {process.ExitCode}" : stderr.Trim());
                if (!File.Exists(outputPath)) throw new InvalidOperationException("provider exited without a preview file");
                item.SetPreviewPath(outputPath);
                StatusText.Text = $"Preview codec ready: {item.DisplayName}";
                return true;
            }
            catch (Exception exception) when (exception is Win32Exception or InvalidOperationException or IOException)
            {
                lastError = exception;
            }
        }
        item.PreviewError = lastError?.Message ?? "Python was not found";
        if (ReferenceEquals(FilesList.SelectedItem, item)) StatusText.Text = item.PreviewError;
        return false;
        }
        finally { _previewSlots.Release(); }
    }

    private void ShowLastImport_Click(object sender, RoutedEventArgs e)
    {
        _catalogView = CatalogView.LastImport;
        _activeCollection = null;
        _activeFolder = null;
        CollectionsList.SelectedItem = null;
        FoldersList.SelectedItem = null;
        ApplyCatalogView();
    }

    private void ShowAllCatalog_Click(object sender, RoutedEventArgs e)
    {
        _catalogView = CatalogView.All;
        _activeCollection = null;
        _activeFolder = null;
        CollectionsList.SelectedItem = null;
        FoldersList.SelectedItem = null;
        ApplyCatalogView();
    }

    private void RefreshCollections()
    {
        var selectedName = (CollectionsList.SelectedItem as CollectionEntry)?.Name;
        _refreshingCollections = true;
        try
        {
            _collections.Clear();
            foreach (var collection in _catalog.Collections)
                _collections.Add(new CollectionEntry(collection.Name, collection.IsTarget, collection.AssetPaths.Count));
            CollectionsList.SelectedItem = _collections.FirstOrDefault(item => item.Name == selectedName);
            TargetText.Text = _catalog.TargetCollection is { } target ? $"B  →  {target}" : T("NoTarget");
        }
        finally { _refreshingCollections = false; }
    }

    private void RefreshFolders()
    {
        var selected = _activeFolder;
        _refreshingCollections = true;
        try
        {
            _folders.Clear();
            foreach (var group in _catalog.Assets.GroupBy(asset => Path.GetDirectoryName(asset.Path) ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                         .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase))
                _folders.Add(new FolderEntry(group.Key, group.Count()));
            FoldersList.SelectedItem = _folders.FirstOrDefault(folder => string.Equals(folder.Path, selected, StringComparison.OrdinalIgnoreCase));
        }
        finally { _refreshingCollections = false; }
    }

    private void ApplyCatalogView()
    {
        IEnumerable<CatalogAsset> assets = _catalog.Assets;
        switch (_catalogView)
        {
            case CatalogView.LastImport:
                var recent = _catalog.LastImportPaths.ToHashSet(StringComparer.OrdinalIgnoreCase);
                assets = assets.Where(asset => recent.Contains(asset.Path));
                StatusText.Text = recent.Count == 0 ? "No successful import session" : $"Showing {recent.Count} photos from the last import";
                break;
            case CatalogView.Collection when _activeCollection is not null:
                var collection = _catalog.Collections.FirstOrDefault(item => string.Equals(item.Name, _activeCollection, StringComparison.OrdinalIgnoreCase));
                var members = collection?.AssetPaths ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                assets = assets.Where(asset => members.Contains(asset.Path));
                StatusText.Text = $"{_activeCollection} · {members.Count} {T("Photos")}";
                break;
            case CatalogView.Folder when _activeFolder is not null:
                assets = assets.Where(asset => string.Equals(Path.GetDirectoryName(asset.Path), _activeFolder, StringComparison.OrdinalIgnoreCase));
                StatusText.Text = $"{_activeFolder} · {assets.Count()} {T("Photos")}";
                break;
            default:
                StatusText.Text = T("ShowAllCatalog");
                break;
        }
        PopulateFiles(assets);
    }

    private async void ImportFolder_Click(object sender, RoutedEventArgs e)
    {
        using var dialog = new Forms.FolderBrowserDialog { Description = T("ImportFolder") };
        if (dialog.ShowDialog() != Forms.DialogResult.OK) return;
        ImportCompletion = ImportDirectoryAsync(dialog.SelectedPath);
        await ImportCompletion;
    }

    public async Task ImportDirectoryAsync(string folder)
    {
        if (_importing) return;
        _importing = true;
        ImportButton.IsEnabled = false;
        ImportProgress.Visibility = Visibility.Visible;
        StatusText.Text = T("Scanning");
        var watch = Stopwatch.StartNew();
        try
        {
            var discovered = await Task.Run(() =>
            {
                var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".heic", ".heif", ".hif", ".arw", ".cr2", ".cr3", ".nef", ".nrw", ".dng" };
                var candidates = Directory.EnumerateFiles(folder, "*", new EnumerationOptions { RecurseSubdirectories = true, IgnoreInaccessible = true })
                    .Where(path => allowed.Contains(Path.GetExtension(path)))
                    .Select(path => {
                        var info = new FileInfo(path);
                        var extension = info.Extension.ToLowerInvariant();
                        return new CatalogAsset(path, extension is ".jpg" or ".jpeg" or ".png" ? "Preview ready" : "Deferred: codec bridge on select",
                            extension, Fingerprint: $"stat:{info.Length}:{info.LastWriteTimeUtc.Ticks}");
                    }).ToArray();
                var rawExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".arw", ".cr2", ".cr3", ".nef", ".nrw", ".dng" };
                return candidates
                    .GroupBy(asset => Path.Combine(Path.GetDirectoryName(asset.Path) ?? string.Empty, Path.GetFileNameWithoutExtension(asset.Path)), StringComparer.OrdinalIgnoreCase)
                    .Select(group => group.FirstOrDefault(asset => rawExtensions.Contains(asset.Extension)) ?? group.First())
                    .ToArray();
            });
            if (_closed) return;
            var rawBases = discovered.Where(asset => asset.Extension is ".arw" or ".cr2" or ".cr3" or ".nef" or ".nrw" or ".dng")
                .Select(asset => Path.Combine(Path.GetDirectoryName(asset.Path) ?? string.Empty, Path.GetFileNameWithoutExtension(asset.Path)))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var obsoleteCompanions = _catalog.Assets
                .Where(asset => rawBases.Contains(Path.Combine(Path.GetDirectoryName(asset.Path) ?? string.Empty, Path.GetFileNameWithoutExtension(asset.Path))))
                .Where(asset => asset.Extension is ".jpg" or ".jpeg" or ".png" or ".heic" or ".heif" or ".hif")
                .Select(asset => asset.Path).ToArray();
            _catalog.RemoveAssets(obsoleteCompanions);
            var imported = discovered.Select(asset => _catalog.UpsertAsset(asset)).ToArray();
            foreach (var asset in imported) _recipes[asset.Path] = asset.Recipe ?? EditRecipe.Default;
            _catalog.RecordLastImport(folder, imported.Select(asset => asset.Path));
            _catalog.Save();
            CollectionsList.SelectedItem = null;
            _catalogView = CatalogView.LastImport;
            _activeCollection = null;
            _activeFolder = null;
            RefreshFolders();
            ApplyCatalogView();
            RefreshLastImportText();
            LibraryTab.IsChecked = true;
            StatusText.Text = string.Format(T("ImportFinished"), imported.Length, watch.Elapsed.TotalSeconds.ToString("0.00"));
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or ArgumentException)
        { StatusText.Text = string.Format(T("ImportFailed"), error.Message); }
        finally
        {
            _importing = false;
            ImportButton.IsEnabled = true;
            ImportProgress.Visibility = Visibility.Collapsed;
        }
    }

    private void CreateCollection_Click(object sender, RoutedEventArgs e)
    {
        var suggestedName = $"Collection {_nextCollection++}";
        while (_catalog.Collections.Any(collection => string.Equals(collection.Name, suggestedName, StringComparison.OrdinalIgnoreCase))) suggestedName = $"Collection {_nextCollection++}";
        var dialog = new CollectionDialog(_i18n, suggestedName) { Owner = this };
        if (dialog.ShowDialog() != true) return;
        try
        {
            var name = _catalog.CreateCollection(dialog.CollectionName, dialog.SetAsTarget);
            var selected = FilesList.SelectedItems.Cast<LibraryItem>().ToArray();
            if (dialog.IncludeSelected) _catalog.AddToCollection(name, selected.Select(item => item.FullPath));
            _catalog.Save();
            RefreshCollections();
            FilesList.Focus();
            StatusText.Text = dialog.IncludeSelected
                ? $"Đã tạo {name}, thêm {selected.Length} ảnh"
                : $"Đã tạo collection: {name}";
        }
        catch (InvalidOperationException exception)
        {
            System.Windows.MessageBox.Show(this, exception.Message, "Không thể tạo collection", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void CollectionsList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (_refreshingCollections || CollectionsList.SelectedItem is not CollectionEntry entry) return;
        _catalogView = CatalogView.Collection;
        _activeCollection = entry.Name;
        _activeFolder = null;
        FoldersList.SelectedItem = null;
        ApplyCatalogView();
    }

    private void FoldersList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (_refreshingCollections || FoldersList.SelectedItem is not FolderEntry folder) return;
        _catalogView = CatalogView.Folder;
        _activeFolder = folder.Path;
        _activeCollection = null;
        CollectionsList.SelectedItem = null;
        ApplyCatalogView();
    }

    private void SetTarget_Click(object sender, RoutedEventArgs e)
    {
        if (CollectionsList.SelectedItem is not CollectionEntry entry) return;
        _catalog.SetTarget(entry.Name);
        _catalog.Save();
        RefreshCollections();
        StatusText.Text = $"B  →  {entry.Name}";
    }

    private async void FilesList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        UpdateCountText();
        var version = ++_selectionVersion;
        _renderVersion++;
        _sourceBitmap = null;
        PreviewImage.Source = null;
        if (FilesList.SelectedItem is not LibraryItem item) return;
        ActivePhotoText.Text = item.DisplayName;
        LoadRecipe(item);
        if (LibraryTab.IsChecked == true) return;
        EmptyPreviewText.Visibility = Visibility.Collapsed;
        try
        {
            if (!item.IsPreviewable && !await GenerateDeferredPreviewAsync(item))
            {
                if (version == _selectionVersion) StatusText.Text = item.PreviewError ?? T("PreviewFailed");
                return;
            }
            var bitmap = await Task.Run(() => LoadDisplayBitmap(item.PreviewSourcePath, 1440));
            if (version != _selectionVersion || _closed) return;
            _sourceBitmap = bitmap;
            RefreshPreview();
        }
        catch (Exception error) when (error is IOException or NotSupportedException or ArgumentException)
        { if (version == _selectionVersion) StatusText.Text = error.Message; }
    }

    private static BitmapSource LoadDisplayBitmap(string path, int maxSide)
    {
        using var stream = File.OpenRead(path);
        var frame = BitmapDecoder.Create(stream, BitmapCreateOptions.DelayCreation, BitmapCacheOption.None).Frames[0];
        var orientation = ReadExifOrientation(frame);
        var width = frame.PixelWidth;
        var height = frame.PixelHeight;
        stream.Position = 0;
        var image = new BitmapImage();
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.StreamSource = stream;
        if (width >= height) image.DecodePixelWidth = Math.Min(width, maxSide);
        else image.DecodePixelHeight = Math.Min(height, maxSide);
        image.EndInit();
        image.Freeze();
        var converted = new FormatConvertedBitmap(image, PixelFormats.Bgra32, null, 0);
        var pixels = new byte[converted.PixelWidth * converted.PixelHeight * 4];
        converted.CopyPixels(pixels, converted.PixelWidth * 4, 0);
        var normalized = PixelOrientation.NormalizeBgra32(pixels, converted.PixelWidth, converted.PixelHeight, orientation);
        var result = BitmapSource.Create(normalized.Width, normalized.Height, 96, 96, PixelFormats.Bgra32, null, normalized.Pixels, normalized.Width * 4);
        result.Freeze();
        return result;
    }

    private void BasicSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (!_uiReady) return;
        ExposureValue.Text = ExposureSlider.Value.ToString("0.00");
        TemperatureValue.Text = ((int)TemperatureSlider.Value).ToString();
        TintValue.Text = ((int)TintSlider.Value).ToString();
        ContrastValue.Text = ((int)ContrastSlider.Value).ToString();
        HighlightsValue.Text = ((int)HighlightsSlider.Value).ToString();
        ShadowsValue.Text = ((int)ShadowsSlider.Value).ToString();
        WhitesValue.Text = ((int)WhitesSlider.Value).ToString();
        BlacksValue.Text = ((int)BlacksSlider.Value).ToString();
        SaturationValue.Text = ((int)SaturationSlider.Value).ToString();
        TextureValue.Text = ((int)TextureSlider.Value).ToString();
        ClarityValue.Text = ((int)ClaritySlider.Value).ToString();
        DehazeValue.Text = ((int)DehazeSlider.Value).ToString();
        VibranceValue.Text = ((int)VibranceSlider.Value).ToString();
        if (!_loadingRecipe && FilesList.SelectedItem is LibraryItem item)
        {
            _recipes[item.FullPath] = CurrentRecipe();
            _catalog.SaveRecipe(item.FullPath, _recipes[item.FullPath]);
            _catalog.Save();
            RefreshPreview();
            StatusText.Text = "Recipe thay đổi (non-destructive preview contract)";
        }
    }

    private void ToneCurveSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (!_uiReady) return;
        UpdateToneCurveValueLabels();
        if (_loadingRecipe || FilesList is null || FilesList.SelectedItem is not LibraryItem item) return;
        _recipes[item.FullPath] = CurrentRecipe();
        _catalog.SaveRecipe(item.FullPath, _recipes[item.FullPath]);
        _catalog.Save();
        RefreshPreview();
        StatusText.Text = "Tone Curve changed (four-region parametric preview)";
    }

    private void UpdateToneCurveValueLabels()
    {
        if (ToneCurveShadowsValue is null) return;
        ToneCurveShadowsValue.Text = ((int)ToneCurveShadowsSlider.Value).ToString(CultureInfo.InvariantCulture);
        ToneCurveDarksValue.Text = ((int)ToneCurveDarksSlider.Value).ToString(CultureInfo.InvariantCulture);
        ToneCurveLightsValue.Text = ((int)ToneCurveLightsSlider.Value).ToString(CultureInfo.InvariantCulture);
        ToneCurveHighlightsValue.Text = ((int)ToneCurveHighlightsSlider.Value).ToString(CultureInfo.InvariantCulture);
    }

    private void ColorMixerChannelChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (!_uiReady) return;
        if (FilesList is null)
        {
            LoadColorMixerControls(ColorMixerAdjustments.Default);
            return;
        }
        LoadColorMixerControls(FilesList.SelectedItem is LibraryItem item && _recipes.TryGetValue(item.FullPath, out var recipe)
            ? recipe.ColorMixer
            : ColorMixerAdjustments.Default);
    }

    private void ColorMixerSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (!_uiReady) return;
        UpdateColorMixerValueLabels();
        if (_loadingRecipe || FilesList is null || FilesList.SelectedItem is not LibraryItem item) return;
        _recipes[item.FullPath] = CurrentRecipe();
        _catalog.SaveRecipe(item.FullPath, _recipes[item.FullPath]);
        _catalog.Save();
        RefreshPreview();
        StatusText.Text = "Color Mixer changed (HSL preview)";
    }

    private ColorMixerAdjustments CurrentColorMixer(ColorMixerAdjustments? existing)
    {
        var source = existing ?? ColorMixerAdjustments.Default;
        var channels = source.Channels is null
            ? new Dictionary<string, ColorMixerChannel>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, ColorMixerChannel>(source.Channels, StringComparer.OrdinalIgnoreCase);
        foreach (var name in ColorMixerAdjustments.ChannelNames)
        {
            if (!channels.ContainsKey(name)) channels[name] = new ColorMixerChannel();
        }
        var active = GetColorMixerChannel();
        channels[active] = new ColorMixerChannel(ColorMixerHueSlider.Value, ColorMixerSaturationSlider.Value, ColorMixerLuminanceSlider.Value);
        return new ColorMixerAdjustments(channels);
    }

    private string GetColorMixerChannel() =>
        (ColorMixerChannelComboBox.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Tag?.ToString() ?? "red";

    private void SelectColorMixerChannel(string channel)
    {
        foreach (var item in ColorMixerChannelComboBox.Items.OfType<System.Windows.Controls.ComboBoxItem>())
        {
            if (string.Equals(item.Tag?.ToString(), channel, StringComparison.OrdinalIgnoreCase))
            {
                ColorMixerChannelComboBox.SelectedItem = item;
                return;
            }
        }
        ColorMixerChannelComboBox.SelectedIndex = 0;
    }

    private void LoadColorMixerControls(ColorMixerAdjustments? mixer)
    {
        if (ColorMixerHueSlider is null) return;
        var channel = (mixer ?? ColorMixerAdjustments.Default).Get(GetColorMixerChannel());
        ColorMixerHueSlider.Value = Math.Clamp(channel.Hue, -100, 100);
        ColorMixerSaturationSlider.Value = Math.Clamp(channel.Saturation, -100, 100);
        ColorMixerLuminanceSlider.Value = Math.Clamp(channel.Luminance, -100, 100);
        UpdateColorMixerValueLabels();
    }

    private void UpdateColorMixerValueLabels()
    {
        if (ColorMixerHueValue is null) return;
        ColorMixerHueValue.Text = ((int)ColorMixerHueSlider.Value).ToString(CultureInfo.InvariantCulture);
        ColorMixerSaturationValue.Text = ((int)ColorMixerSaturationSlider.Value).ToString(CultureInfo.InvariantCulture);
        ColorMixerLuminanceValue.Text = ((int)ColorMixerLuminanceSlider.Value).ToString(CultureInfo.InvariantCulture);
    }

    private void LensCorrectionOptionChanged(object sender, RoutedEventArgs e)
    {
        if (!_uiReady) return;
        UpdateLensCorrectionValueLabels();
        if (LensCorrectionStatus is not null && LensProfileEnabledCheckBox is not null)
        {
            LensCorrectionStatus.Text = LensProfileEnabledCheckBox.IsChecked == true
                ? "Profile selection is recorded; manual distortion and vignetting are applied."
                : "Manual distortion and vignetting are applied; calibrated profile remains disabled.";
        }
        if (_loadingRecipe || FilesList is null || FilesList.SelectedItem is not LibraryItem item) return;
        _recipes[item.FullPath] = CurrentRecipe();
        _catalog.SaveRecipe(item.FullPath, _recipes[item.FullPath]);
        _catalog.Save();
        RefreshPreview();
        StatusText.Text = "Lens correction settings changed";
    }

    private LensCorrectionSettings CurrentLensCorrection(LensCorrectionSettings? existing)
    {
        return new LensCorrectionSettings(
            GetLensProfile(),
            LensProfileEnabledCheckBox.IsChecked == true,
            LensDistortionSlider.Value,
            LensVignettingSlider.Value);
    }

    private string GetLensProfile() =>
        (LensProfileComboBox.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content?.ToString() ?? "manual";

    private void SelectLensProfile(string profile)
    {
        foreach (var item in LensProfileComboBox.Items.OfType<System.Windows.Controls.ComboBoxItem>())
        {
            if (string.Equals(item.Content?.ToString(), profile, StringComparison.OrdinalIgnoreCase))
            {
                LensProfileComboBox.SelectedItem = item;
                return;
            }
        }
        LensProfileComboBox.SelectedIndex = 0;
    }

    private void UpdateLensCorrectionValueLabels()
    {
        if (LensDistortionValue is null) return;
        LensDistortionValue.Text = ((int)LensDistortionSlider.Value).ToString(CultureInfo.InvariantCulture);
        LensVignettingValue.Text = ((int)LensVignettingSlider.Value).ToString(CultureInfo.InvariantCulture);
    }

    private EditRecipe CurrentRecipe()
    {
        var existing = FilesList.SelectedItem is LibraryItem item && _recipes.TryGetValue(item.FullPath, out var recipe)
            ? recipe
            : EditRecipe.Default;
        MaskReference? mask = null;
        var maskPath = MaskPathText.Text;
        if (!string.IsNullOrWhiteSpace(maskPath) && !IsNoMaskText(maskPath))
        {
            mask = new MaskReference(
                "raster",
                maskPath,
                Basic: new BasicAdjustments(Exposure: MaskExposureSlider.Value),
                Invert: MaskInvertCheckBox.IsChecked == true);
        }
        WatermarkSettings? watermark = null;
        var watermarkText = WatermarkTextBox.Text.Trim();
        var watermarkWidth = ParseOptionalPositiveInt(ExportWidthTextBox.Text);
        var watermarkHeight = ParseOptionalPositiveInt(ExportHeightTextBox.Text);
        if (watermarkText.Length > 0 || watermarkWidth.HasValue || watermarkHeight.HasValue)
        {
            watermark = new WatermarkSettings(
                watermarkText,
                WatermarkOpacitySlider.Value,
                24,
                GetWatermarkPosition(),
                watermarkWidth,
                watermarkHeight);
        }
        return existing with
        {
            Basic = new BasicAdjustments(ExposureSlider.Value, ContrastSlider.Value, HighlightsSlider.Value, ShadowsSlider.Value, WhitesSlider.Value, BlacksSlider.Value, SaturationSlider.Value,
                TemperatureSlider.Value, TintSlider.Value, TextureSlider.Value, ClaritySlider.Value, DehazeSlider.Value, VibranceSlider.Value),
            ToneCurve = new ToneCurveAdjustments(ToneCurveShadowsSlider.Value, ToneCurveDarksSlider.Value, ToneCurveLightsSlider.Value, ToneCurveHighlightsSlider.Value),
            ColorMixer = CurrentColorMixer(existing.ColorMixer),
            LensCorrection = CurrentLensCorrection(existing.LensCorrection),
            Crop = CurrentCrop(existing.Crop ?? new CropRect()),
            Mask = mask,
            Watermark = watermark
        };
    }

    private void LoadRecipe(LibraryItem item)
    {
        if (!_recipes.TryGetValue(item.FullPath, out var recipe)) recipe = EditRecipe.Default;
        _loadingRecipe = true;
        try
        {
            ExposureSlider.Value = recipe.Basic.Exposure;
            TemperatureSlider.Value = recipe.Basic.Temperature;
            TintSlider.Value = recipe.Basic.Tint;
            ContrastSlider.Value = recipe.Basic.Contrast;
            HighlightsSlider.Value = recipe.Basic.Highlights;
            ShadowsSlider.Value = recipe.Basic.Shadows;
            WhitesSlider.Value = recipe.Basic.Whites;
            BlacksSlider.Value = recipe.Basic.Blacks;
            SaturationSlider.Value = recipe.Basic.Saturation;
            TextureSlider.Value = recipe.Basic.Texture;
            ClaritySlider.Value = recipe.Basic.Clarity;
            DehazeSlider.Value = recipe.Basic.Dehaze;
            VibranceSlider.Value = recipe.Basic.Vibrance;
            var toneCurve = recipe.ToneCurve ?? new ToneCurveAdjustments();
            ToneCurveShadowsSlider.Value = toneCurve.Shadows;
            ToneCurveDarksSlider.Value = toneCurve.Darks;
            ToneCurveLightsSlider.Value = toneCurve.Lights;
            ToneCurveHighlightsSlider.Value = toneCurve.Highlights;
            UpdateToneCurveValueLabels();
            SelectColorMixerChannel("red");
            LoadColorMixerControls(recipe.ColorMixer);
            SelectLensProfile(recipe.LensCorrection?.ProfileId ?? "manual");
            LensProfileEnabledCheckBox.IsChecked = recipe.LensCorrection?.ProfileEnabled == true;
            LensDistortionSlider.Value = Math.Clamp(recipe.LensCorrection?.Distortion ?? 0, -100, 100);
            LensVignettingSlider.Value = Math.Clamp(recipe.LensCorrection?.Vignetting ?? 0, -100, 100);
            UpdateLensCorrectionValueLabels();
            var crop = recipe.Crop ?? new CropRect();
            SelectCropAspect(crop.Aspect);
            CropAngleSlider.Value = Math.Clamp(crop.Angle, CropAngleSlider.Minimum, CropAngleSlider.Maximum);
            CropAngleValue.Text = CropAngleSlider.Value.ToString("0.0", CultureInfo.InvariantCulture);
            MaskPathText.Text = recipe.Mask?.Path ?? T("NoMaskSelected");
            MaskInvertCheckBox.IsChecked = recipe.Mask?.Invert == true;
            MaskExposureSlider.Value = recipe.Mask?.Basic?.Exposure ?? 0;
            WatermarkTextBox.Text = recipe.Watermark?.Text ?? string.Empty;
            WatermarkOpacitySlider.Value = recipe.Watermark?.Opacity ?? 65;
            WatermarkOpacityValue.Text = WatermarkOpacitySlider.Value.ToString("0");
            SelectWatermarkPosition(recipe.Watermark?.Position ?? "bottom_right");
            ExportWidthTextBox.Text = (recipe.Watermark?.Width ?? 0).ToString(CultureInfo.InvariantCulture);
            ExportHeightTextBox.Text = (recipe.Watermark?.Height ?? 0).ToString(CultureInfo.InvariantCulture);
        }
        finally { _loadingRecipe = false; }
    }

    private void MaskSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (!_uiReady) return;
        MaskExposureValue.Text = MaskExposureSlider.Value.ToString("0.00");
        if (!_loadingRecipe && FilesList.SelectedItem is LibraryItem)
        {
            _recipes[((LibraryItem)FilesList.SelectedItem).FullPath] = CurrentRecipe();
            _catalog.SaveRecipe(((LibraryItem)FilesList.SelectedItem).FullPath, _recipes[((LibraryItem)FilesList.SelectedItem).FullPath]);
            _catalog.Save();
            RefreshPreview();
            StatusText.Text = "Mask recipe changed (non-destructive preview contract)";
        }
    }

    private void CropOptionChanged(object sender, RoutedEventArgs e)
    {
        if (!_uiReady) return;
        CropAngleValue.Text = CropAngleSlider.Value.ToString("0.0", CultureInfo.InvariantCulture);
        if (_loadingRecipe || FilesList is null || FilesList.SelectedItem is not LibraryItem item) return;
        _recipes[item.FullPath] = CurrentRecipe();
        _catalog.SaveRecipe(item.FullPath, _recipes[item.FullPath]);
        _catalog.Save();
        RefreshPreview();
        StatusText.Text = "Crop aspect/angle changed";
    }

    private void ResetCrop_Click(object sender, RoutedEventArgs e)
    {
        _loadingRecipe = true;
        try
        {
            SelectCropAspect("original");
            CropAngleSlider.Value = 0;
        }
        finally { _loadingRecipe = false; }
        CropOptionChanged(sender, e);
    }

    private void AutoStraighten_Click(object sender, RoutedEventArgs e)
    {
        if (_sourceBitmap is null || FilesList.SelectedItem is not LibraryItem)
        {
            StatusText.Text = "Select an image before auto straighten";
            return;
        }
        var source = new FormatConvertedBitmap(_sourceBitmap, PixelFormats.Bgra32, null, 0);
        var stride = source.PixelWidth * 4;
        var pixels = new byte[stride * source.PixelHeight];
        source.CopyPixels(pixels, stride, 0);
        if (!HorizonStraightener.TryEstimateCorrectionDegrees(pixels, source.PixelWidth, source.PixelHeight, out var correction))
        {
            StatusText.Text = "Auto straighten could not find a reliable near-horizontal edge";
            return;
        }
        _loadingRecipe = true;
        try { CropAngleSlider.Value = Math.Clamp(correction, CropAngleSlider.Minimum, CropAngleSlider.Maximum); }
        finally { _loadingRecipe = false; }
        CropOptionChanged(sender, e);
        StatusText.Text = $"Auto horizon correction applied: {correction.ToString("0.0", CultureInfo.InvariantCulture)}°";
    }

    private CropRect CurrentCrop(CropRect existing)
    {
        var aspect = GetCropAspect();
        if (string.Equals(aspect, "original", StringComparison.OrdinalIgnoreCase))
            return existing with { Angle = CropAngleSlider.Value, Aspect = "original" };
        var sourceWidth = _sourceBitmap?.PixelWidth ?? 1;
        var sourceHeight = _sourceBitmap?.PixelHeight ?? 1;
        var targetRatio = aspect switch
        {
            "1:1" => 1.0,
            "4:3" => 4.0 / 3.0,
            "3:2" => 3.0 / 2.0,
            "16:9" => 16.0 / 9.0,
            _ => sourceWidth / (double)sourceHeight
        };
        var sourceRatio = sourceWidth / (double)sourceHeight;
        var left = 0.0;
        var top = 0.0;
        var right = 1.0;
        var bottom = 1.0;
        if (targetRatio > sourceRatio)
        {
            var normalizedHeight = sourceRatio / targetRatio;
            top = (1.0 - normalizedHeight) / 2.0;
            bottom = top + normalizedHeight;
        }
        else if (targetRatio < sourceRatio)
        {
            var normalizedWidth = targetRatio / sourceRatio;
            left = (1.0 - normalizedWidth) / 2.0;
            right = left + normalizedWidth;
        }
        return new CropRect(left, top, right, bottom, CropAngleSlider.Value, aspect);
    }

    private string GetCropAspect() => (CropAspectComboBox.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Tag?.ToString() ?? "original";

    private void SelectCropAspect(string aspect)
    {
        foreach (var item in CropAspectComboBox.Items.OfType<System.Windows.Controls.ComboBoxItem>())
        {
            if (string.Equals(item.Tag?.ToString(), aspect, StringComparison.OrdinalIgnoreCase))
            {
                CropAspectComboBox.SelectedItem = item;
                return;
            }
        }
        CropAspectComboBox.SelectedIndex = 0;
    }

    private void MaskOptionChanged(object sender, RoutedEventArgs e)
    {
        if (!_uiReady) return;
        if (_loadingRecipe || FilesList.SelectedItem is not LibraryItem item) return;
        _recipes[item.FullPath] = CurrentRecipe();
        _catalog.SaveRecipe(item.FullPath, _recipes[item.FullPath]);
        _catalog.Save();
        RefreshPreview();
        StatusText.Text = "Mask option changed (subject/background complement)";
    }

    private void ChooseMask_Click(object sender, RoutedEventArgs e)
    {
        using var dialog = new Forms.OpenFileDialog
        {
            Title = "Choose a raster mask",
            Filter = "Mask/image files|*.png;*.jpg;*.jpeg;*.tif;*.tiff;*.bmp|All files|*.*",
            CheckFileExists = true
        };
        if (dialog.ShowDialog() != Forms.DialogResult.OK) return;
        MaskPathText.Text = dialog.FileName;
        MaskOptionChanged(sender, e);
    }

    private void PaintMask_Click(object sender, RoutedEventArgs e)
    {
        if (_sourceBitmap is null || FilesList.SelectedItem is not LibraryItem item || !item.IsPreviewable)
        {
            StatusText.Text = "Select a previewable JPEG/PNG before painting a mask";
            return;
        }
        var dialog = new MaskBrushDialog(_i18n, _sourceBitmap) { Owner = this };
        if (dialog.ShowDialog() != true || string.IsNullOrWhiteSpace(dialog.MaskPath)) return;
        MaskPathText.Text = dialog.MaskPath;
        MaskOptionChanged(sender, e);
        StatusText.Text = "Brush mask saved and attached to the selected photo";
    }

    private async void AutoSubjectMask_Click(object sender, RoutedEventArgs e)
    {
        if (_sourceBitmap is null || FilesList.SelectedItem is not LibraryItem item || !item.IsPreviewable)
        {
            StatusText.Text = "Select a previewable image before generating a subject mask";
            return;
        }
        var script = Path.Combine(AppContext.BaseDirectory, "tools", "generate_subject_mask.py");
        var model = Path.Combine(AppContext.BaseDirectory, "models", "modnet_photographic.onnx");
        if (!File.Exists(script) || !File.Exists(model))
        {
            StatusText.Text = "Auto mask provider is unavailable: Python script or MODNet model is missing";
            return;
        }
        var outputDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MisaImageEditor", "masks");
        Directory.CreateDirectory(outputDirectory);
        var outputPath = Path.Combine(outputDirectory, $"subject-mask-{Guid.NewGuid():N}.png");
        StatusText.Text = "Generating offline subject mask...";
        var pythonCandidates = new[]
        {
            Environment.GetEnvironmentVariable("MISA_PYTHON"),
            "python.exe",
            "python"
        }.Where(candidate => !string.IsNullOrWhiteSpace(candidate)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        Exception? lastError = null;
        foreach (var python in pythonCandidates)
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = python!,
                    WorkingDirectory = AppContext.BaseDirectory,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
                startInfo.ArgumentList.Add(script);
                startInfo.ArgumentList.Add(item.PreviewSourcePath);
                startInfo.ArgumentList.Add(model);
                startInfo.ArgumentList.Add(outputPath);
                using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Could not start Python");
                var stdoutTask = process.StandardOutput.ReadToEndAsync();
                var stderrTask = process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();
                var stderr = await stderrTask;
                _ = await stdoutTask;
                if (process.ExitCode != 0)
                    throw new InvalidOperationException(string.IsNullOrWhiteSpace(stderr) ? $"provider exit code {process.ExitCode}" : stderr.Trim());
                if (!File.Exists(outputPath)) throw new InvalidOperationException("provider exited without a mask file");
                MaskPathText.Text = outputPath;
                MaskInvertCheckBox.IsChecked = false;
                MaskOptionChanged(sender, e);
                StatusText.Text = "Offline subject mask generated and attached; check Invert for background work";
                return;
            }
            catch (Exception exception) when (exception is Win32Exception or InvalidOperationException or IOException)
            {
                lastError = exception;
            }
        }
        StatusText.Text = $"Auto mask unavailable: {lastError?.Message ?? "Python was not found"}";
    }

    private void ClearMask_Click(object sender, RoutedEventArgs e)
    {
        MaskPathText.Text = T("NoMaskSelected");
        MaskInvertCheckBox.IsChecked = false;
        MaskExposureSlider.Value = 0;
        if (FilesList.SelectedItem is not LibraryItem item) return;
        _recipes[item.FullPath] = CurrentRecipe();
        _catalog.SaveRecipe(item.FullPath, _recipes[item.FullPath]);
        _catalog.Save();
        RefreshPreview();
        StatusText.Text = "Mask cleared";
    }

    private void ExportOptionsChanged(object sender, RoutedEventArgs e)
    {
        if (!_uiReady) return;
        WatermarkOpacityValue.Text = WatermarkOpacitySlider.Value.ToString("0");
        UpdateWatermarkOverlay();
        if (_loadingRecipe || FilesList.SelectedItem is not LibraryItem item) return;
        _recipes[item.FullPath] = CurrentRecipe();
        _catalog.SaveRecipe(item.FullPath, _recipes[item.FullPath]);
        _catalog.Save();
    }

    private void ExportCurrent_Click(object sender, RoutedEventArgs e)
    {
        if (FilesList.SelectedItem is not LibraryItem item || !item.IsPreviewable )
        {
            StatusText.Text = "Select a previewable JPEG/PNG before exporting";
            return;
        }
        using var dialog = new Forms.SaveFileDialog
        {
            Title = "Export image",
            Filter = "JPEG image|*.jpg;*.jpeg|PNG image|*.png",
            DefaultExt = ".jpg",
            AddExtension = true,
            FileName = Path.GetFileNameWithoutExtension(item.FullPath) + "-edited"
        };
        if (dialog.ShowDialog() != Forms.DialogResult.OK) return;

        var source = RenderRecipe(LoadBitmap(item.PreviewSourcePath), CurrentRecipe());
        var (width, height) = ResolveOutputSize(source.PixelWidth, source.PixelHeight);
        BitmapSource renderSource = source;
        if (width != source.PixelWidth || height != source.PixelHeight)
            renderSource = new TransformedBitmap(source, new ScaleTransform((double)width / source.PixelWidth, (double)height / source.PixelHeight));

        var visual = new DrawingVisual();
        using (var context = visual.RenderOpen())
        {
            context.DrawImage(renderSource, new Rect(0, 0, width, height));
            DrawWatermark(context, width, height);
        }
        var output = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
        output.Render(visual);
        BitmapEncoder encoder = Path.GetExtension(dialog.FileName).Equals(".png", StringComparison.OrdinalIgnoreCase)
            ? new PngBitmapEncoder()
            : new JpegBitmapEncoder { QualityLevel = 95 };
        encoder.Frames.Add(BitmapFrame.Create(output));
        using (var stream = File.Create(dialog.FileName)) encoder.Save(stream);

        _recipes[item.FullPath] = CurrentRecipe();
        _catalog.SaveRecipe(item.FullPath, _recipes[item.FullPath]);
        _catalog.Save();
        StatusText.Text = $"Exported {Path.GetFileName(dialog.FileName)} ({width}x{height})";
    }

    private void ExportSelected_Click(object sender, RoutedEventArgs e)
    {
        var selected = FilesList.SelectedItems.Cast<LibraryItem>().ToArray();
        if (selected.Length == 0)
        {
            StatusText.Text = "Select one or more photos before batch export";
            return;
        }
        using var dialog = new Forms.FolderBrowserDialog { Description = "Choose the parent folder for batch export" };
        if (dialog.ShowDialog() != Forms.DialogResult.OK) return;

        string outputFolder;
        try
        {
            outputFolder = ResolveBatchOutputFolder(dialog.SelectedPath, ExportSubfolderTextBox.Text);
            Directory.CreateDirectory(outputFolder);
        }
        catch (Exception exception) when (exception is ArgumentException or IOException or UnauthorizedAccessException)
        {
            StatusText.Text = $"Batch export folder is invalid: {exception.Message}";
            return;
        }

        var png = ExportFormatComboBox.SelectedIndex == 1;
        var exported = 0;
        var skipped = 0;
        foreach (var item in selected)
        {
            if (!item.IsPreviewable)
            {
                skipped++;
                continue;
            }
            try
            {
                var source = LoadBitmap(item.PreviewSourcePath);
                var recipe = _recipes.TryGetValue(item.FullPath, out var saved) ? saved : EditRecipe.Default;
                var rendered = RenderRecipe(source, recipe);
                var extension = png ? ".png" : ".jpg";
                var outputPath = Path.Combine(outputFolder, Path.GetFileNameWithoutExtension(item.FullPath) + "-edited" + extension);
                outputPath = MakeUniquePath(outputPath);
                ExportBitmap(rendered, outputPath, recipe.Watermark, png);
                exported++;
            }
            catch (Exception exception) when (exception is not OutOfMemoryException and not StackOverflowException)
            {
                skipped++;
                StatusText.Text = $"Skipped {item.DisplayName}: {exception.Message}";
            }
        }
        StatusText.Text = $"Batch export finished: {exported} exported, {skipped} skipped to {outputFolder}";
    }

    private static string ResolveBatchOutputFolder(string parentFolder, string? subfolder)
    {
        var root = Path.GetFullPath(parentFolder);
        var cleanSubfolder = (subfolder ?? string.Empty).Trim();
        if (cleanSubfolder.Length == 0) return root;
        if (Path.IsPathRooted(cleanSubfolder)) throw new ArgumentException("Subfolder must be relative");
        var segments = cleanSubfolder.Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);
        if (segments.Any(segment => segment is "." or ".." || segment.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0))
            throw new ArgumentException("Subfolder contains an invalid path segment");
        var output = Path.GetFullPath(Path.Combine(root, Path.Combine(segments)));
        var rootWithSeparator = root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (!output.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase)) throw new ArgumentException("Subfolder escapes the selected parent folder");
        return output;
    }

    private static string MakeUniquePath(string path)
    {
        if (!File.Exists(path)) return path;
        var directory = Path.GetDirectoryName(path) ?? string.Empty;
        var stem = Path.GetFileNameWithoutExtension(path);
        var extension = Path.GetExtension(path);
        for (var index = 2; index < 10000; index++)
        {
            var candidate = Path.Combine(directory, $"{stem}-{index}{extension}");
            if (!File.Exists(candidate)) return candidate;
        }
        throw new IOException("Could not allocate a unique output filename");
    }

    private static BitmapSource LoadBitmap(string path)
    {
        using var stream = File.OpenRead(path);
        var decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
        var frame = decoder.Frames[0];
        var orientation = ReadExifOrientation(frame);
        if (orientation == 1) return frame;

        var converted = new FormatConvertedBitmap(frame, PixelFormats.Bgra32, null, 0);
        var stride = converted.PixelWidth * 4;
        var pixels = new byte[stride * converted.PixelHeight];
        converted.CopyPixels(pixels, stride, 0);
        var normalized = PixelOrientation.NormalizeBgra32(pixels, converted.PixelWidth, converted.PixelHeight, orientation);
        var bitmap = new WriteableBitmap(normalized.Width, normalized.Height, 96, 96, PixelFormats.Bgra32, null);
        bitmap.WritePixels(new Int32Rect(0, 0, normalized.Width, normalized.Height), normalized.Pixels, normalized.Width * 4, 0);
        bitmap.Freeze();
        return bitmap;
    }

    private static int ReadExifOrientation(BitmapFrame frame)
    {
        if (frame.Metadata is not BitmapMetadata metadata) return 1;
        try
        {
            return metadata.GetQuery("/app1/ifd/{ushort=274}") switch
            {
                ushort value => value,
                short value => value,
                byte value => value,
                uint value => (int)value,
                ulong value => (int)value,
                _ => 1
            };
        }
        catch (InvalidOperationException)
        {
            return 1;
        }
    }

    private static BitmapSource RenderRecipe(BitmapSource bitmap, EditRecipe recipe)
    {
        var source = new FormatConvertedBitmap(bitmap, PixelFormats.Bgra32, null, 0);
        int width = source.PixelWidth;
        int height = source.PixelHeight;
        int stride = width * 4;
        var pixels = new byte[stride * height];
        source.CopyPixels(pixels, stride, 0);
        var basic = recipe.Basic;
        var toneCurve = recipe.ToneCurve ?? new ToneCurveAdjustments();
        var exposure = Math.Pow(2.0, basic.Exposure);
        var contrast = 1.0 + basic.Contrast / 100.0;
        var saturation = 1.0 + basic.Saturation / 100.0;
        var temperature = basic.Temperature / 100.0;
        var tint = basic.Tint / 100.0;
        var microContrast = 1.0 + (basic.Texture * 0.25 + basic.Clarity * 0.35 + basic.Dehaze * 0.40) / 100.0;
        var vibrance = basic.Vibrance / 100.0;
        var shadows = basic.Shadows / 100.0;
        var highlights = basic.Highlights / 100.0;
        var whites = basic.Whites / 100.0;
        var blacks = basic.Blacks / 100.0;
        for (int index = 0; index < pixels.Length; index += 4)
        {
            double blue = pixels[index] / 255.0;
            double green = pixels[index + 1] / 255.0;
            double red = pixels[index + 2] / 255.0;
            // Temperature and tint are RGB-domain white-balance controls. They are
            // deliberately applied before tone controls, as in a RAW workflow.
            red *= 1.0 + temperature * 0.18 + tint * 0.04;
            blue *= 1.0 - temperature * 0.18 - tint * 0.04;
            green *= 1.0 - tint * 0.10;
            red = (red * exposure - 0.5) * contrast + 0.5;
            green = (green * exposure - 0.5) * contrast + 0.5;
            blue = (blue * exposure - 0.5) * contrast + 0.5;
            var luma = red * 0.2126 + green * 0.7152 + blue * 0.0722;
            var shadowWeight = Math.Clamp(1.0 - luma * 2.0, 0.0, 1.0);
            var darkWeight = Math.Clamp(1.0 - Math.Abs(luma - 0.25) * 4.0, 0.0, 1.0);
            var lightWeight = Math.Clamp(1.0 - Math.Abs(luma - 0.75) * 4.0, 0.0, 1.0);
            var highlightWeight = Math.Clamp((luma - 0.5) * 2.0, 0.0, 1.0);
            var curveDelta = toneCurve.Shadows / 100.0 * shadowWeight * 0.22 +
                             toneCurve.Darks / 100.0 * darkWeight * 0.16 +
                             toneCurve.Lights / 100.0 * lightWeight * 0.16 +
                             toneCurve.Highlights / 100.0 * highlightWeight * 0.22;
            red += curveDelta;
            green += curveDelta;
            blue += curveDelta;
            if (shadows != 0)
            {
                var weight = Math.Clamp(1.0 - luma * 2.0, 0.0, 1.0) * shadows * 0.35;
                red += weight; green += weight; blue += weight;
            }
            if (highlights != 0)
            {
                var weight = Math.Clamp((luma - 0.5) * 2.0, 0.0, 1.0) * highlights * 0.35;
                red += weight; green += weight; blue += weight;
            }
            if (whites != 0)
            {
                var weight = Math.Clamp((luma - 0.65) / 0.35, 0.0, 1.0) * whites * 0.25;
                red += weight; green += weight; blue += weight;
            }
            if (blacks != 0)
            {
                var weight = Math.Clamp((0.35 - luma) / 0.35, 0.0, 1.0) * blacks * 0.25;
                red += weight; green += weight; blue += weight;
            }
            red = luma + (red - luma) * saturation;
            green = luma + (green - luma) * saturation;
            blue = luma + (blue - luma) * saturation;
            // Texture, clarity and dehaze share an intentionally bounded
            // mid-tone contrast operation in this CPU preview pipeline.
            red = (red - 0.5) * microContrast + 0.5;
            green = (green - 0.5) * microContrast + 0.5;
            blue = (blue - 0.5) * microContrast + 0.5;
            var maxChannel = Math.Max(red, Math.Max(green, blue));
            var minChannel = Math.Min(red, Math.Min(green, blue));
            var chroma = Math.Clamp(maxChannel - minChannel, 0, 1);
            var vibranceScale = 1.0 + vibrance * (1.0 - chroma);
            var adjustedLuma = red * 0.2126 + green * 0.7152 + blue * 0.0722;
            red = adjustedLuma + (red - adjustedLuma) * vibranceScale;
            green = adjustedLuma + (green - adjustedLuma) * vibranceScale;
            blue = adjustedLuma + (blue - adjustedLuma) * vibranceScale;
            pixels[index] = ToByte(blue);
            pixels[index + 1] = ToByte(green);
            pixels[index + 2] = ToByte(red);
        }
        ApplyColorMixer(pixels, width, height, recipe.ColorMixer ?? ColorMixerAdjustments.Default);
        ApplyVignetting(pixels, width, height, recipe.LensCorrection);
        pixels = ApplyDistortion(pixels, width, height, recipe.LensCorrection);
        ApplyRasterMask(pixels, width, height, recipe.Mask);
        var preview = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);
        preview.WritePixels(new Int32Rect(0, 0, width, height), pixels, stride, 0);
        preview.Freeze();
        return ApplyCrop(preview, recipe.Crop ?? new CropRect());
    }

    private static void ApplyVignetting(byte[] pixels, int width, int height, LensCorrectionSettings? settings)
    {
        var amount = settings?.Vignetting ?? 0;
        if (Math.Abs(amount) < 0.001) return;
        var centerX = (width - 1) / 2.0;
        var centerY = (height - 1) / 2.0;
        var radiusX = Math.Max(1.0, centerX);
        var radiusY = Math.Max(1.0, centerY);
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var dx = (x - centerX) / radiusX;
                var dy = (y - centerY) / radiusY;
                var radiusSquared = Math.Min(1.0, dx * dx + dy * dy);
                var factor = Math.Clamp(1.0 - amount / 100.0 * radiusSquared * 0.65, 0.1, 2.0);
                var index = (y * width + x) * 4;
                pixels[index] = ToByte(pixels[index] / 255.0 * factor);
                pixels[index + 1] = ToByte(pixels[index + 1] / 255.0 * factor);
                pixels[index + 2] = ToByte(pixels[index + 2] / 255.0 * factor);
            }
        }
    }

    private static byte[] ApplyDistortion(byte[] pixels, int width, int height, LensCorrectionSettings? settings)
    {
        var amount = settings?.Distortion ?? 0;
        return PixelWarp.ApplyRadialDistortion(pixels, width, height, amount);
    }

    private static void ApplyColorMixer(byte[] pixels, int width, int height, ColorMixerAdjustments mixer)
    {
        if (ColorMixerAdjustments.ChannelNames.All(name => mixer.Get(name) is { Hue: 0, Saturation: 0, Luminance: 0 })) return;
        for (var index = 0; index < pixels.Length; index += 4)
        {
            var red = pixels[index + 2] / 255.0;
            var green = pixels[index + 1] / 255.0;
            var blue = pixels[index] / 255.0;
            RgbToHsl(red, green, blue, out var hue, out var saturation, out var luminance);
            var channelName = HueToChannel(hue);
            var adjustment = mixer.Get(channelName);
            if (adjustment.Hue == 0 && adjustment.Saturation == 0 && adjustment.Luminance == 0) continue;
            hue = (hue + adjustment.Hue / 100.0) % 1.0;
            if (hue < 0) hue += 1.0;
            saturation = Math.Clamp(saturation + adjustment.Saturation / 100.0, 0, 1);
            luminance = Math.Clamp(luminance + adjustment.Luminance / 100.0, 0, 1);
            HslToRgb(hue, saturation, luminance, out red, out green, out blue);
            pixels[index] = ToByte(blue);
            pixels[index + 1] = ToByte(green);
            pixels[index + 2] = ToByte(red);
        }
    }

    private static string HueToChannel(double hue)
    {
        var degrees = hue * 360.0;
        var names = ColorMixerAdjustments.ChannelNames;
        var centers = new[] { 0.0, 30.0, 60.0, 120.0, 180.0, 240.0, 280.0, 320.0 };
        var best = 0;
        var distance = double.MaxValue;
        for (var index = 0; index < centers.Length; index++)
        {
            var delta = Math.Abs(degrees - centers[index]);
            delta = Math.Min(delta, 360.0 - delta);
            if (delta < distance) { distance = delta; best = index; }
        }
        return names[best];
    }

    private static void RgbToHsl(double red, double green, double blue, out double hue, out double saturation, out double luminance)
    {
        var max = Math.Max(red, Math.Max(green, blue));
        var min = Math.Min(red, Math.Min(green, blue));
        var delta = max - min;
        luminance = (max + min) / 2.0;
        if (delta < 0.000001)
        {
            hue = 0;
            saturation = 0;
            return;
        }
        saturation = luminance > 0.5 ? delta / (2.0 - max - min) : delta / (max + min);
        hue = max == red
            ? (green - blue) / delta + (green < blue ? 6.0 : 0.0)
            : max == green
                ? (blue - red) / delta + 2.0
                : (red - green) / delta + 4.0;
        hue /= 6.0;
    }

    private static void HslToRgb(double hue, double saturation, double luminance, out double red, out double green, out double blue)
    {
        if (saturation < 0.000001)
        {
            red = green = blue = luminance;
            return;
        }
        var q = luminance < 0.5 ? luminance * (1.0 + saturation) : luminance + saturation - luminance * saturation;
        var p = 2.0 * luminance - q;
        red = HueToRgb(p, q, hue + 1.0 / 3.0);
        green = HueToRgb(p, q, hue);
        blue = HueToRgb(p, q, hue - 1.0 / 3.0);
    }

    private static double HueToRgb(double p, double q, double t)
    {
        if (t < 0) t += 1;
        if (t > 1) t -= 1;
        if (t < 1.0 / 6.0) return p + (q - p) * 6.0 * t;
        if (t < 1.0 / 2.0) return q;
        if (t < 2.0 / 3.0) return p + (q - p) * (2.0 / 3.0 - t) * 6.0;
        return p;
    }

    private static BitmapSource ApplyCrop(BitmapSource source, CropRect crop)
    {
        var left = Math.Clamp(crop.Left, 0, 1);
        var top = Math.Clamp(crop.Top, 0, 1);
        var right = Math.Clamp(crop.Right, 0, 1);
        var bottom = Math.Clamp(crop.Bottom, 0, 1);
        var x = Math.Clamp((int)Math.Round(left * source.PixelWidth), 0, Math.Max(0, source.PixelWidth - 1));
        var y = Math.Clamp((int)Math.Round(top * source.PixelHeight), 0, Math.Max(0, source.PixelHeight - 1));
        var x2 = Math.Clamp((int)Math.Round(right * source.PixelWidth), x + 1, source.PixelWidth);
        var y2 = Math.Clamp((int)Math.Round(bottom * source.PixelHeight), y + 1, source.PixelHeight);
        BitmapSource result = new CroppedBitmap(source, new Int32Rect(x, y, x2 - x, y2 - y));
        if (Math.Abs(crop.Angle) > 0.001)
            result = new TransformedBitmap(result, new RotateTransform(crop.Angle));
        result.Freeze();
        return result;
    }

    private static void ApplyRasterMask(byte[] pixels, int width, int height, MaskReference? mask)
    {
        if (mask is null || string.IsNullOrWhiteSpace(mask.Path) || !File.Exists(mask.Path)) return;
        var path = mask.Path;
        try
        {
            var bitmap = LoadBitmap(path);
            var scaled = new TransformedBitmap(bitmap, new ScaleTransform((double)width / bitmap.PixelWidth, (double)height / bitmap.PixelHeight));
            var gray = new FormatConvertedBitmap(scaled, PixelFormats.Gray8, null, 0);
            var maskPixels = new byte[width * height];
            gray.CopyPixels(maskPixels, width, 0);
            var localExposure = Math.Pow(2.0, mask.Basic?.Exposure ?? 0);
            for (int pixel = 0, maskIndex = 0; pixel < pixels.Length; pixel += 4, maskIndex++)
            {
                var alpha = maskPixels[maskIndex] / 255.0;
                if (mask.Invert) alpha = 1.0 - alpha;
                if (alpha <= 0) continue;
                var blue = pixels[pixel] / 255.0;
                var green = pixels[pixel + 1] / 255.0;
                var red = pixels[pixel + 2] / 255.0;
                pixels[pixel] = ToByte(blue * (1.0 - alpha) + Math.Clamp(blue * localExposure, 0, 1) * alpha);
                pixels[pixel + 1] = ToByte(green * (1.0 - alpha) + Math.Clamp(green * localExposure, 0, 1) * alpha);
                pixels[pixel + 2] = ToByte(red * (1.0 - alpha) + Math.Clamp(red * localExposure, 0, 1) * alpha);
            }
        }
        catch (IOException)
        {
            // A missing or locked mask must not abort the rest of a batch.
        }
    }

    private static void ExportBitmap(BitmapSource source, string path, WatermarkSettings? settings, bool png)
    {
        var (width, height) = ResolveOutputSize(source.PixelWidth, source.PixelHeight, settings);
        BitmapSource renderSource = source;
        if (width != source.PixelWidth || height != source.PixelHeight)
            renderSource = new TransformedBitmap(source, new ScaleTransform((double)width / source.PixelWidth, (double)height / source.PixelHeight));
        var visual = new DrawingVisual();
        using (var context = visual.RenderOpen())
        {
            context.DrawImage(renderSource, new Rect(0, 0, width, height));
            DrawWatermark(context, width, height, settings);
        }
        var output = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
        output.Render(visual);
        BitmapEncoder encoder = png ? new PngBitmapEncoder() : new JpegBitmapEncoder { QualityLevel = 95 };
        encoder.Frames.Add(BitmapFrame.Create(output));
        using var stream = File.Create(path);
        encoder.Save(stream);
    }

    private static (int Width, int Height) ResolveOutputSize(int sourceWidth, int sourceHeight, WatermarkSettings? settings)
    {
        var requestedWidth = settings?.Width ?? 0;
        var requestedHeight = settings?.Height ?? 0;
        if (requestedWidth == 0 && requestedHeight == 0) return (sourceWidth, sourceHeight);
        if (requestedWidth == 0) requestedWidth = Math.Max(1, (int)Math.Round(sourceWidth * (double)requestedHeight / sourceHeight));
        if (requestedHeight == 0) requestedHeight = Math.Max(1, (int)Math.Round(sourceHeight * (double)requestedWidth / sourceWidth));
        return (requestedWidth, requestedHeight);
    }

    private static void DrawWatermark(DrawingContext context, int width, int height, WatermarkSettings? settings)
    {
        var text = settings?.Text?.Trim() ?? string.Empty;
        var opacity = settings?.Opacity ?? 0;
        if (text.Length == 0 || opacity <= 0) return;
        var alpha = (byte)Math.Round(Math.Clamp(opacity, 0, 100) / 100.0 * 255.0);
        var brush = new SolidColorBrush(System.Windows.Media.Color.FromArgb(alpha, 255, 255, 255));
        brush.Freeze();
        var fontSize = Math.Max(12, Math.Min(48, width / 28.0));
        var formatted = new FormattedText(text, CultureInfo.CurrentCulture, System.Windows.FlowDirection.LeftToRight, new Typeface("Segoe UI"), fontSize, brush, 1.0);
        var margin = settings?.Margin ?? 24;
        var position = settings?.Position ?? "bottom_right";
        var x = position switch
        {
            "top_right" or "bottom_right" => width - formatted.Width - margin,
            "center" => (width - formatted.Width) / 2.0,
            _ => margin
        };
        var y = position switch
        {
            "bottom_left" or "bottom_right" => height - formatted.Height - margin,
            "center" => (height - formatted.Height) / 2.0,
            _ => margin
        };
        context.DrawText(formatted, new System.Windows.Point(Math.Max(0, x), Math.Max(0, y)));
    }

    private (int Width, int Height) ResolveOutputSize(int sourceWidth, int sourceHeight)
    {
        var requestedWidth = ParseOptionalPositiveInt(ExportWidthTextBox.Text) ?? 0;
        var requestedHeight = ParseOptionalPositiveInt(ExportHeightTextBox.Text) ?? 0;
        if (requestedWidth == 0 && requestedHeight == 0) return (sourceWidth, sourceHeight);
        if (requestedWidth == 0) requestedWidth = Math.Max(1, (int)Math.Round(sourceWidth * (double)requestedHeight / sourceHeight));
        if (requestedHeight == 0) requestedHeight = Math.Max(1, (int)Math.Round(sourceHeight * (double)requestedWidth / sourceWidth));
        return (requestedWidth, requestedHeight);
    }

    private void DrawWatermark(DrawingContext context, int width, int height)
    {
        var text = WatermarkTextBox.Text.Trim();
        if (text.Length == 0 || WatermarkOpacitySlider.Value <= 0) return;
        var alpha = (byte)Math.Round(Math.Clamp(WatermarkOpacitySlider.Value, 0, 100) / 100.0 * 255.0);
        var brush = new SolidColorBrush(System.Windows.Media.Color.FromArgb(alpha, 255, 255, 255));
        brush.Freeze();
        var fontSize = Math.Max(12, Math.Min(48, width / 28.0));
        var formatted = new FormattedText(text, CultureInfo.CurrentCulture, System.Windows.FlowDirection.LeftToRight, new Typeface("Segoe UI"), fontSize, brush, 1.0);
        var margin = 24.0;
        var position = GetWatermarkPosition();
        var x = position switch
        {
            "top_right" or "bottom_right" => width - formatted.Width - margin,
            "center" => (width - formatted.Width) / 2.0,
            _ => margin
        };
        var y = position switch
        {
            "bottom_left" or "bottom_right" => height - formatted.Height - margin,
            "center" => (height - formatted.Height) / 2.0,
            _ => margin
        };
        context.DrawText(formatted, new System.Windows.Point(Math.Max(0, x), Math.Max(0, y)));
    }

    private void UpdateWatermarkOverlay()
    {
        if (PreviewWatermarkText is null) return;
        var text = WatermarkTextBox.Text.Trim();
        PreviewWatermarkText.Text = text;
        PreviewWatermarkText.Opacity = Math.Clamp(WatermarkOpacitySlider.Value, 0, 100) / 100.0;
        PreviewWatermarkText.Visibility = text.Length == 0 || PreviewImage.Source is null ? Visibility.Collapsed : Visibility.Visible;
        var position = GetWatermarkPosition();
        PreviewWatermarkText.HorizontalAlignment = position switch
        {
            "top_right" or "bottom_right" => System.Windows.HorizontalAlignment.Right,
            "center" => System.Windows.HorizontalAlignment.Center,
            _ => System.Windows.HorizontalAlignment.Left
        };
        PreviewWatermarkText.VerticalAlignment = position switch
        {
            "bottom_left" or "bottom_right" => VerticalAlignment.Bottom,
            "center" => VerticalAlignment.Center,
            _ => VerticalAlignment.Top
        };
        PreviewWatermarkText.Margin = position == "center" ? new Thickness(0) : new Thickness(24);
    }

    private string GetWatermarkPosition() => (WatermarkPositionComboBox.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Tag?.ToString() ?? "bottom_right";

    private void SelectWatermarkPosition(string position)
    {
        foreach (var item in WatermarkPositionComboBox.Items.OfType<System.Windows.Controls.ComboBoxItem>())
        {
            if (string.Equals(item.Tag?.ToString(), position, StringComparison.OrdinalIgnoreCase))
            {
                WatermarkPositionComboBox.SelectedItem = item;
                return;
            }
        }
        WatermarkPositionComboBox.SelectedIndex = 4;
    }

    private static int? ParseOptionalPositiveInt(string text)
    {
        return int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) && value > 0 ? value : null;
    }

    private void RefreshPreview()
    {
        _renderVersion++;
        _renderTimer.Stop();
        if (_sourceBitmap is not null) _renderTimer.Start();
    }

    private async void RenderPreviewNow()
    {
        if (_sourceBitmap is null || _closed) return;
        var version = _renderVersion;
        var source = _sourceBitmap;
        var recipe = CurrentRecipe();
        try
        {
            var rendered = await Task.Run(() => RenderRecipe(source, recipe));
            if (version != _renderVersion || _closed) return;
            PreviewImage.Source = rendered;
            UpdateWatermarkOverlay();
        }
        catch (Exception error) when (error is IOException or ArgumentException or InvalidOperationException)
        { if (version == _renderVersion) StatusText.Text = error.Message; }
    }

    private static byte ToByte(double value) => (byte)Math.Round(Math.Clamp(value, 0, 1) * 255.0);

    private void CopySettings_Click(object sender, RoutedEventArgs e)
    {
        if (FilesList.SelectedItem is not LibraryItem source)
        {
            StatusText.Text = "Hãy chọn ảnh nguồn trước khi copy settings";
            return;
        }
        var dialog = new CopySettingsDialog(_i18n) { Owner = this };
        if (dialog.ShowDialog() != true) return;
        var selected = SelectedPresetFields(dialog);
        _recipes[source.FullPath] = CurrentRecipe();
        _catalog.SaveRecipe(source.FullPath, _recipes[source.FullPath]);
        _catalog.Save();
        _clipboard = new RecipeCopy(FilesList.SelectedIndex, selected, _recipes[source.FullPath]);
        StatusText.Text = $"Đã copy {selected.Count} nhóm/trường vào clipboard nội bộ";
    }

    private static HashSet<string> SelectedPresetFields(CopySettingsDialog dialog)
    {
        var selected = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (dialog.CopyBasic)
        {
            selected.UnionWith(new[]
            {
                "basic.exposure", "basic.contrast", "basic.highlights", "basic.shadows",
                "basic.whites", "basic.blacks", "basic.saturation"
            });
        }
        if (dialog.CopyToneCurve) selected.Add("tone_curve");
        if (dialog.CopyColorMixer) selected.Add("color_mixer");
        if (dialog.CopyLensCorrection) selected.Add("lens_correction");
        if (dialog.CopyCrop) selected.Add("crop");
        if (dialog.CopyMask) selected.Add("mask");
        if (dialog.CopyWatermark) selected.Add("watermark");
        return selected;
    }

    private void SavePreset_Click(object sender, RoutedEventArgs e)
    {
        if (FilesList.SelectedItem is not LibraryItem source)
        {
            StatusText.Text = "Hãy chọn ảnh nguồn trước khi lưu preset";
            return;
        }
        var selection = new CopySettingsDialog(_i18n) { Owner = this };
        if (selection.ShowDialog() != true) return;
        var fields = SelectedPresetFields(selection);
        _recipes[source.FullPath] = CurrentRecipe();
        _catalog.SaveRecipe(source.FullPath, _recipes[source.FullPath]);
        var dialog = new Forms.SaveFileDialog
        {
            Title = "Lưu preset MISA",
            Filter = "MISA preset (*.json)|*.json",
            DefaultExt = "json",
            AddExtension = true,
            InitialDirectory = _presetStore.DirectoryPath,
            FileName = "MISA preset.json",
            OverwritePrompt = true
        };
        if (dialog.ShowDialog() != Forms.DialogResult.OK) return;
        try
        {
            var preset = EditPreset.Create(Path.GetFileNameWithoutExtension(dialog.FileName), fields, _recipes[source.FullPath]);
            var path = _presetStore.Save(preset, dialog.FileName);
            _clipboard = preset.ToRecipeCopy();
            _catalog.Save();
            StatusText.Text = $"Đã lưu preset: {Path.GetFileName(path)}";
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or InvalidDataException)
        {
            StatusText.Text = $"Không thể lưu preset: {exception.Message}";
        }
    }

    private void ApplyPreset_Click(object sender, RoutedEventArgs e)
    {
        var selected = FilesList.SelectedItems.Cast<LibraryItem>().ToArray();
        if (selected.Length == 0)
        {
            StatusText.Text = "Hãy chọn ít nhất một ảnh đích để áp dụng preset";
            return;
        }
        var dialog = new Forms.OpenFileDialog
        {
            Title = "Áp dụng preset MISA",
            Filter = "MISA preset (*.json)|*.json",
            InitialDirectory = _presetStore.DirectoryPath,
            CheckFileExists = true,
            Multiselect = false
        };
        if (dialog.ShowDialog() != Forms.DialogResult.OK) return;
        try
        {
            var preset = _presetStore.Load(dialog.FileName);
            var copy = preset.ToRecipeCopy();
            foreach (var item in selected)
            {
                var destination = _recipes.TryGetValue(item.FullPath, out var current) ? current : EditRecipe.Default;
                _recipes[item.FullPath] = copy.ApplyTo(destination);
                _catalog.SaveRecipe(item.FullPath, _recipes[item.FullPath]);
            }
            _clipboard = copy;
            _catalog.Save();
            if (FilesList.SelectedItem is LibraryItem active) LoadRecipe(active);
            StatusText.Text = $"Đã áp dụng preset {preset.Name} vào {selected.Length} ảnh";
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or InvalidDataException or JsonException)
        {
            StatusText.Text = $"Không thể đọc preset: {exception.Message}";
        }
    }

    private void PasteSettings_Click(object sender, RoutedEventArgs e)
    {
        if (_clipboard is null) { StatusText.Text = "Chưa có copy settings"; return; }
        var selected = FilesList.SelectedItems.Cast<LibraryItem>().ToArray();
        if (selected.Length == 0) { StatusText.Text = "Select one or more destination photos"; return; }
        foreach (var item in selected)
        {
            var destination = _recipes.TryGetValue(item.FullPath, out var current) ? current : EditRecipe.Default;
            _recipes[item.FullPath] = _clipboard.ApplyTo(destination);
            _catalog.SaveRecipe(item.FullPath, _recipes[item.FullPath]);
        }
        _catalog.Save();
        if (FilesList.SelectedItem is LibraryItem active) LoadRecipe(active);
        StatusText.Text = $"Đã paste Basic recipe vào {selected.Length} ảnh";
    }

    private void AddToTarget_Click(object sender, RoutedEventArgs e) => AddSelectedToTarget();

    public void AddSelectedToTarget()
    {
        if (_catalog.TargetCollection is null) { StatusText.Text = T("NoTarget"); return; }
        var selected = FilesList.SelectedItems.Cast<LibraryItem>().ToArray();
        if (selected.Length == 0) { StatusText.Text = T("SelectPhotos"); return; }
        var added = _catalog.AddToTarget(selected.Select(item => item.FullPath));
        _catalog.Save();
        RefreshCollections();
        StatusText.Text = string.Format(T("AddedToTarget"), added, selected.Length, _catalog.TargetCollection);
    }

    private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key != Key.B || Keyboard.Modifiers != ModifierKeys.None || Keyboard.FocusedElement is System.Windows.Controls.Primitives.TextBoxBase) return;
        if (e.IsRepeat) { e.Handled = true; return; }
        AddSelectedToTarget();
        e.Handled = true;
    }

    private void LibraryTab_Checked(object sender, RoutedEventArgs e)
    {
        if (_uiReady) SetWorkspace(false);
    }

    private void EditorTab_Checked(object sender, RoutedEventArgs e)
    {
        if (!_uiReady) return;
        SetWorkspace(true);
        FilesList_SelectionChanged(FilesList, new System.Windows.Controls.SelectionChangedEventArgs(System.Windows.Controls.Primitives.Selector.SelectionChangedEvent, Array.Empty<object>(), Array.Empty<object>()));
    }

    private void OpenEditor_DoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (FilesList.SelectedItem is not null) EditorTab.IsChecked = true;
    }

    private void SetWorkspace(bool editor)
    {
        PreviewRow.Height = editor ? new GridLength(1, GridUnitType.Star) : new GridLength(0);
        BrowserRow.Height = editor ? new GridLength(190) : new GridLength(1, GridUnitType.Star);
        EditorColumn.Width = editor ? new GridLength(320) : new GridLength(0);
        EditorScroll.Visibility = editor ? Visibility.Visible : Visibility.Collapsed;
        PreviewBorder.Visibility = editor ? Visibility.Visible : Visibility.Collapsed;
        if (!editor) { _selectionVersion++; _renderVersion++; _sourceBitmap = null; }
    }

    protected override void OnClosed(EventArgs e)
    {
        _closed = true;
        _thumbnailCancellation?.Cancel();
        _renderTimer.Stop();
        _catalog.Save();
        base.OnClosed(e);
    }
}

public sealed class LibraryItem : INotifyPropertyChanged
{
    private string _status;
    private string? _previewPath;

    public LibraryItem(string fullPath, string status, string extension, string? previewPath = null)
    {
        FullPath = fullPath;
        Extension = extension;
        _previewPath = previewPath;
        _status = previewPath is null ? status : "Preview ready (codec bridge)";
    }

    public string FullPath { get; }
    public string Status => _previewError is not null ? "⚠" : Extension.TrimStart('.').ToUpperInvariant();
    private string? _previewError;
    public string? PreviewError { get => _previewError; set { _previewError = value; OnPropertyChanged(nameof(Status)); } }
    private BitmapSource? _thumbnail;
    public BitmapSource? Thumbnail { get => _thumbnail; set { _thumbnail = value; OnPropertyChanged(nameof(Thumbnail)); } }
    public string Extension { get; }
    public string DisplayName => Path.GetFileName(FullPath);
    public bool IsCodecDeferred => _previewPath is null && _status.StartsWith("Deferred:", StringComparison.OrdinalIgnoreCase);
    public bool IsPreviewable => File.Exists(PreviewSourcePath);
    public string PreviewSourcePath => _previewPath ?? (_status == "Preview ready" ? FullPath : string.Empty);
    public string? PreviewPath => IsPreviewable ? PreviewSourcePath : null;

    public void SetPreviewPath(string path)
    {
        _previewPath = path;
        _status = "Preview ready (codec bridge)";
        OnPropertyChanged(nameof(Status));
        OnPropertyChanged(nameof(PreviewPath));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

public sealed record CollectionEntry(string Name, bool IsTarget, int Count)
{
    public string DisplayName => $"{(IsTarget ? "●  " : "○  ")}{Name}   {Count}";
}

public sealed record FolderEntry(string Path, int Count)
{
    public string DisplayName => $"{System.IO.Path.GetFileName(Path)}   {Count}";
}

public enum CatalogView { All, LastImport, Collection, Folder }
