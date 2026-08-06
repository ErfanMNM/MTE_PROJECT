using System.Text.Json;

namespace DMS_Project.Config;

/// <summary>
/// Handles loading and saving configuration to JSON file using Reflection.
/// </summary>
public static class ConfigStorage
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Gets the default configuration file path.
    /// </summary>
    public static string DefaultFilePath => Path.Combine(
        AppContext.BaseDirectory,
        "configs.json"
    );

    /// <summary>
    /// Saves the configuration object to a JSON file.
    /// Uses Reflection to iterate all public properties.
    /// </summary>
    public static void Save<T>(T config, string? filePath = null) where T : class
    {
        filePath ??= DefaultFilePath;
        var json = JsonSerializer.Serialize(config, JsonOptions);
        File.WriteAllText(filePath, json);
    }

    /// <summary>
    /// Loads configuration from a JSON file.
    /// Uses Reflection to map JSON properties back to the config object.
    /// </summary>
    public static T Load<T>(string? filePath = null) where T : class, new()
    {
        filePath ??= DefaultFilePath;

        if (!File.Exists(filePath))
            return new T();

        var json = File.ReadAllText(filePath);
        return JsonSerializer.Deserialize<T>(json, JsonOptions) ?? new T();
    }

    /// <summary>
    /// Checks if a configuration file exists.
    /// </summary>
    public static bool Exists(string? filePath = null)
    {
        return File.Exists(filePath ?? DefaultFilePath);
    }
}

