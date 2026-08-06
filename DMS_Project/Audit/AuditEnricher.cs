using System.Reflection;
using System.Text.Json;

namespace DMS_Project.Audit;

public static class AuditEnricher
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static string? ToJson(object? value)
    {
        if (value == null) return null;
        try
        {
            return JsonSerializer.Serialize(value, JsonOptions);
        }
        catch
        {
            return JsonSerializer.Serialize(value.ToString(), JsonOptions);
        }
    }

    public static string? DiffFieldNames(object? before, object? after)
    {
        if (before == null || after == null) return null;
        var beforeType = before.GetType();
        var afterType = after.GetType();
        if (beforeType != afterType) return null;

        var changed = new List<string>();
        foreach (var prop in beforeType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!prop.CanRead) continue;
            var a = prop.GetValue(before);
            var b = prop.GetValue(after);
            if (!Equals(Normalize(a), Normalize(b)))
                changed.Add(prop.Name);
        }
        return changed.Count == 0 ? null : string.Join(",", changed);
    }

    private static object? Normalize(object? value)
    {
        if (value is null) return null;
        if (value is string s) return s ?? string.Empty;
        return value;
    }
}