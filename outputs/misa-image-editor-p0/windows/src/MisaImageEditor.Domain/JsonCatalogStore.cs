using System.Security.Cryptography;
using System.Text.Json;

namespace MisaImageEditor.Domain;

public sealed record CatalogAsset(
    string Path,
    string Status,
    string Extension,
    int Width = 0,
    int Height = 0,
    int Orientation = 1,
    EditRecipe? Recipe = null,
    string? Fingerprint = null);

public sealed record CatalogCollection(string Name, bool IsTarget, IReadOnlySet<string> AssetPaths);

public sealed class JsonCatalogStore
{
    private sealed class State
    {
        public int SchemaVersion { get; set; } = 1;
        public List<CatalogAsset> Assets { get; set; } = new();
        public List<CollectionState> Collections { get; set; } = new();
        public string? TargetCollection { get; set; }
        public string? LastImportFolder { get; set; }
        public DateTimeOffset? LastImportAt { get; set; }
        public List<string> LastImportPaths { get; set; } = new();
    }

    private sealed class CollectionState
    {
        public string Name { get; set; } = string.Empty;
        public HashSet<string> AssetPaths { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    }

    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };
    private readonly State _state;

    public JsonCatalogStore(string filePath)
    {
        FilePath = filePath;
        _state = Load(filePath);
    }

    public string FilePath { get; }
    public IReadOnlyList<CatalogAsset> Assets => _state.Assets;
    public IReadOnlyList<CatalogCollection> Collections => _state.Collections
        .Select(collection => new CatalogCollection(collection.Name, string.Equals(collection.Name, _state.TargetCollection, StringComparison.OrdinalIgnoreCase), collection.AssetPaths))
        .ToArray();
    public string? TargetCollection => _state.TargetCollection;
    public IReadOnlyList<string> LastImportPaths => _state.LastImportPaths;

    public CatalogAsset UpsertAsset(CatalogAsset asset)
    {
        var normalized = System.IO.Path.GetFullPath(asset.Path);
        var existing = _state.Assets.FindIndex(item => string.Equals(item.Path, normalized, StringComparison.OrdinalIgnoreCase));
        var previous = existing >= 0 ? _state.Assets[existing] : null;
        var fingerprint = asset.Fingerprint;
        if (string.IsNullOrWhiteSpace(fingerprint) && File.Exists(normalized)) fingerprint = ComputeFingerprint(normalized);
        var value = asset with
        {
            Path = normalized,
            Recipe = asset.Recipe ?? previous?.Recipe ?? EditRecipe.Default,
            Fingerprint = fingerprint ?? previous?.Fingerprint
        };
        if (existing >= 0)
        {
            _state.Assets[existing] = value;
        }
        else _state.Assets.Add(value);
        return value;
    }

    public static string ComputeFingerprint(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
    }

    public void RecordLastImport(string folder, IEnumerable<string> importedPaths)
    {
        var paths = importedPaths.Select(System.IO.Path.GetFullPath).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        if (paths.Count == 0) return;
        _state.LastImportFolder = System.IO.Path.GetFullPath(folder);
        _state.LastImportAt = DateTimeOffset.Now;
        _state.LastImportPaths = paths;
    }

    public string CreateCollection(string name, bool setAsTarget)
    {
        var clean = name.Trim();
        if (clean.Length == 0) throw new ArgumentException("Collection name is required", nameof(name));
        if (_state.Collections.Any(item => string.Equals(item.Name, clean, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException($"Collection already exists: {clean}");
        _state.Collections.Add(new CollectionState { Name = clean });
        if (setAsTarget) _state.TargetCollection = clean;
        return clean;
    }

    public void SetTarget(string name)
    {
        var collection = _state.Collections.FirstOrDefault(item => string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase));
        if (collection is null) throw new InvalidOperationException($"Collection does not exist: {name}");
        _state.TargetCollection = collection.Name;
    }

    public int AddToTarget(IEnumerable<string> paths)
    {
        if (_state.TargetCollection is null) throw new InvalidOperationException("No target collection is set");
        return AddToCollection(_state.TargetCollection, paths);
    }

    public int AddToCollection(string name, IEnumerable<string> paths)
    {
        var collection = _state.Collections.FirstOrDefault(item => string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase));
        if (collection is null) throw new InvalidOperationException($"Collection does not exist: {name}");
        var before = collection.AssetPaths.Count;
        foreach (var path in paths) collection.AssetPaths.Add(System.IO.Path.GetFullPath(path));
        return collection.AssetPaths.Count - before;
    }

    public EditRecipe LoadRecipe(string path)
    {
        var normalized = System.IO.Path.GetFullPath(path);
        return _state.Assets.FirstOrDefault(item => string.Equals(item.Path, normalized, StringComparison.OrdinalIgnoreCase))?.Recipe ?? EditRecipe.Default;
    }

    public void SaveRecipe(string path, EditRecipe recipe)
    {
        var normalized = System.IO.Path.GetFullPath(path);
        var index = _state.Assets.FindIndex(item => string.Equals(item.Path, normalized, StringComparison.OrdinalIgnoreCase));
        if (index < 0) throw new KeyNotFoundException(normalized);
        _state.Assets[index] = _state.Assets[index] with { Recipe = recipe };
    }

    public void Save()
    {
        var directory = System.IO.Path.GetDirectoryName(FilePath);
        if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);
        var temporary = FilePath + ".tmp";
        File.WriteAllText(temporary, JsonSerializer.Serialize(_state, _jsonOptions));
        File.Move(temporary, FilePath, true);
    }

    private State Load(string filePath)
    {
        if (!File.Exists(filePath)) return new State();
        try
        {
            var state = JsonSerializer.Deserialize<State>(File.ReadAllText(filePath), _jsonOptions);
            return state is { SchemaVersion: 1 } ? state : new State();
        }
        catch (JsonException)
        {
            return new State();
        }
    }
}
