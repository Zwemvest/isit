using System.Text.RegularExpressions;
using IsIt.Models;

namespace IsIt.Services;

public static partial class QuizDataParser
{
    [GeneratedRegex(@"^(?<name>[^|]+)\s*\|\s*(?<categories>.+)$")]
    private static partial Regex EntryPattern();

    [GeneratedRegex(@"(?<cat>\w+)(?<suffix>[~?!]?):\s*""(?<desc>(?:[^""\\]|\\.)*)""")]
    private static partial Regex CategoryPattern();

    public static List<QuizItem> Parse(string content)
    {
        var items = new List<QuizItem>();

        foreach (var line in content.Split('\n'))
        {
            var trimmed = line.Trim();
            if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith('#'))
                continue;

            var match = EntryPattern().Match(trimmed);
            if (!match.Success) continue;

            var item = new QuizItem
            {
                Name = match.Groups["name"].Value.Trim(),
                Categories = ParseCategories(match.Groups["categories"].Value)
            };

            items.Add(item);
        }

        return items;
    }

    private static List<CategoryEntry> ParseCategories(string categoriesStr)
    {
        var categories = new List<CategoryEntry>();

        foreach (Match match in CategoryPattern().Matches(categoriesStr))
        {
            var suffix = match.Groups["suffix"].Value;
            var result = suffix switch
            {
                "~" => ResultType.Arguable,
                "?" => ResultType.Obscure,
                "!" => ResultType.Miss,
                _ => ResultType.Correct
            };

            categories.Add(new CategoryEntry
            {
                Category = match.Groups["cat"].Value,
                Result = result,
                Description = Unescape(match.Groups["desc"].Value)
            });
        }

        return categories;
    }

    private static string Unescape(string s) => s.Replace("\\\"", "\"").Replace("\\\\", "\\");
}
