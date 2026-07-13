using System.Net;
using System.Text.Json;
using System.Web;

namespace TwitchIrcMinimal;

public static class Translator
{
    private static readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(5) };

    public static (string effects, bool translated) NeedTranslate(string? effects)
    {
        if (string.IsNullOrEmpty(effects)) return (effects ?? "", false);
        var parts = effects.Split('-', StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < parts.Length; i++)
        {
            if (parts[i].Equals("tr", StringComparison.OrdinalIgnoreCase))
            {
                var result = parts.Where((_, idx) => idx != i).ToArray();
                return (string.Join("-", result), true);
            }
        }
        return (effects, false);
    }

    public static async Task<string?> TranslateAsync(string lang, string text)
    {
        var query = HttpUtility.ParseQueryString("");
        query["client"] = "gtx";
        query["dt"] = "t";
        query["sl"] = "auto";
        query["tl"] = lang;
        query["q"] = text;
        var url = $"https://translate.googleapis.com/translate_a/single?{query}";

        try
        {
            var resp = await _http.GetAsync(url);
            if (resp.StatusCode != HttpStatusCode.OK) return null;

            var json = await resp.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.GetArrayLength() == 0) return null;

            var firstLevel = root[0];
            var result = "";
            for (int i = 0; i < firstLevel.GetArrayLength(); i++)
            {
                var segment = firstLevel[i];
                if (segment.GetArrayLength() > 0)
                    result += segment[0].GetString() ?? "";
            }
            return result;
        }
        catch { return null; }
    }
}
