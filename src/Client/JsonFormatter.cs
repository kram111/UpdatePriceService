using System.Text.Json;

namespace src.Client;

/// <summary>
/// Форматировщик JSON
/// </summary>
public static class JsonFormatter
{
    private static readonly JsonSerializerOptions _options = new() { WriteIndented = true };
    /// <summary>
    /// Формирует JSON строку
    /// </summary>
    /// <param name="json"></param>
    /// <returns></returns>
    public static string PrettyPrint(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            return JsonSerializer.Serialize(doc, _options);
        }
        catch
        {
            return json;
        }
    }
}