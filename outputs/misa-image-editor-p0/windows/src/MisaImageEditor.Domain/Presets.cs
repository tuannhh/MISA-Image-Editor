using System.Text.Json;
using System.Text.Json.Serialization;

namespace MisaImageEditor.Domain;

public sealed record EditPreset(
    [property: JsonPropertyName("schema_version")] int SchemaVersion,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("selected_fields")] IReadOnlyList<string> SelectedFields,
    [property: JsonPropertyName("recipe")] EditRecipe Recipe)
{
    public static EditPreset Create(string name, IEnumerable<string> selectedFields, EditRecipe recipe)
    {
        var cleanName = name.Trim();
        if (cleanName.Length == 0) throw new ArgumentException("Preset name is required", nameof(name));
        var fields = new HashSet<string>(selectedFields.Where(field => !string.IsNullOrWhiteSpace(field)), StringComparer.OrdinalIgnoreCase);
        if (fields.Count == 0) throw new ArgumentException("A preset must contain at least one selected field", nameof(selectedFields));
        return new EditPreset(1, cleanName, fields.OrderBy(field => field, StringComparer.OrdinalIgnoreCase).ToArray(), recipe);
    }

    public RecipeCopy ToRecipeCopy() => new(0, new HashSet<string>(SelectedFields, StringComparer.OrdinalIgnoreCase), Recipe);
}

public sealed class JsonPresetStore
{
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public JsonPresetStore(string directory)
    {
        DirectoryPath = directory;
        Directory.CreateDirectory(directory);
    }

    public string DirectoryPath { get; }

    public IReadOnlyList<string> ListFiles() =>
        Directory.Exists(DirectoryPath)
            ? Directory.EnumerateFiles(DirectoryPath, "*.json", SearchOption.TopDirectoryOnly).OrderBy(path => path, StringComparer.OrdinalIgnoreCase).ToArray()
            : Array.Empty<string>();

    public string Save(EditPreset preset, string? filePath = null)
    {
        var target = string.IsNullOrWhiteSpace(filePath)
            ? Path.Combine(DirectoryPath, SanitizeFileName(preset.Name) + ".json")
            : Path.GetFullPath(filePath);
        var directory = Path.GetDirectoryName(target) ?? DirectoryPath;
        Directory.CreateDirectory(directory);
        var temporary = target + ".tmp";
        File.WriteAllText(temporary, JsonSerializer.Serialize(preset, _jsonOptions));
        File.Move(temporary, target, true);
        return target;
    }

    public EditPreset Load(string filePath)
    {
        var target = Path.GetFullPath(filePath);
        var preset = JsonSerializer.Deserialize<EditPreset>(File.ReadAllText(target), _jsonOptions)
            ?? throw new InvalidDataException("Preset file is empty");
        if (preset.SchemaVersion != 1) throw new InvalidDataException($"Unsupported preset schema: {preset.SchemaVersion}");
        return EditPreset.Create(preset.Name, preset.SelectedFields, preset.Recipe);
    }

    public void Delete(string filePath)
    {
        var target = Path.GetFullPath(filePath);
        File.Delete(target);
    }

    private static string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var clean = new string(name.Select(character => invalid.Contains(character) ? '_' : character).ToArray()).Trim();
        return clean.Length == 0 ? "Preset" : clean;
    }
}
