// Copyright (c) Microsoft. All rights reserved.

using System.Text.Json;
using System.Text.RegularExpressions;

namespace OnboardRoom.Application.Utilities;

public static partial class JsonRepair
{
    public static T? TryDeserializeFromPossiblyFencedJson<T>(string text, JsonSerializerOptions? options = null)
    {
        string candidate = ExtractJsonObject(text);
        candidate = TrailingCommaRegex().Replace(candidate, "$1");
        JsonSerializerOptions serializerOptions = options ?? new JsonSerializerOptions(JsonSerializerDefaults.Web);

        try
        {
            return JsonSerializer.Deserialize<T>(candidate, serializerOptions);
        }
        catch (JsonException)
        {
            return default;
        }
    }

    public static string ExtractJsonObject(string text)
    {
        Match fenced = FencedJsonRegex().Match(text);
        if (fenced.Success)
        {
            return fenced.Groups["json"].Value.Trim();
        }

        int start = text.IndexOf('{');
        int end = text.LastIndexOf('}');
        return start >= 0 && end > start
            ? text[start..(end + 1)]
            : text.Trim();
    }

    [GeneratedRegex("```(?:json)?\\s*(?<json>[\\s\\S]*?)\\s*```", RegexOptions.IgnoreCase)]
    private static partial Regex FencedJsonRegex();

    [GeneratedRegex(",\\s*([}\\]])")]
    private static partial Regex TrailingCommaRegex();
}
