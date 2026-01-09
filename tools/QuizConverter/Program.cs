using System.Text.Json;

var baseDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
var inputDir = Path.Combine(baseDir, "data-backup", "quiz-items-json");
var outputDir = Path.Combine(baseDir, "wwwroot", "data", "quiz-items");

Console.WriteLine($"Base dir: {baseDir}");
Console.WriteLine($"Input dir: {inputDir}");
Console.WriteLine($"Output dir: {outputDir}");

Directory.CreateDirectory(outputDir);

var files = new[] { "mythology", "fantasy", "anime-games", "music", "tech", "people", "things" };

foreach (var file in files)
{
    Console.WriteLine($"Converting {file}.json...");

    var jsonPath = Path.Combine(inputDir, $"{file}.json");
    var quizPath = Path.Combine(outputDir, $"{file}.quiz");

    if (!File.Exists(jsonPath))
    {
        Console.WriteLine($"  ERROR: {jsonPath} not found!");
        continue;
    }

    var json = File.ReadAllText(jsonPath);
    var items = JsonSerializer.Deserialize<List<QuizItem>>(json, new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true
    }) ?? [];

    // Sort alphabetically by name
    items = items.OrderBy(i => i.Name, StringComparer.OrdinalIgnoreCase).ToList();

    var lines = new List<string>();

    // Header
    lines.Add("# ============================================");
    lines.Add($"# QUIZ DATA - {char.ToUpper(file[0]) + file.Substring(1)}");
    lines.Add("# ============================================");
    lines.Add("# Format: Name | Category: \"Description\" | ...");
    lines.Add("#");
    lines.Add("# Result suffixes:");
    lines.Add("#   (none) = Correct   - Player must select this");
    lines.Add("#   ~      = Arguable  - Acceptable, but debatable");
    lines.Add("#   ?      = Obscure   - Acceptable, trivia-level");
    lines.Add("#   !      = Miss      - Wrong, but commonly confused");
    lines.Add("# ============================================");
    lines.Add("");

    foreach (var item in items)
    {
        var categories = item.Categories
            // Order: Correct (0) -> Arguable (1) -> Obscure (2) -> Miss (-1)
            .OrderBy(c => c.Result switch
            {
                0 => 0,   // Correct first
                1 => 1,   // Arguable second
                2 => 2,   // Obscure third
                -1 => 3,  // Miss last
                _ => 4
            })
            .ThenBy(c => c.Category)
            .Select(c =>
            {
                var suffix = c.Result switch
                {
                    1 => "~",
                    2 => "?",
                    -1 => "!",
                    _ => ""
                };
                var desc = EscapeDescription(c.Description);
                return $"{c.Category}{suffix}: \"{desc}\"";
            });

        var line = $"{item.Name} | {string.Join(" | ", categories)}";
        lines.Add(line);
    }

    File.WriteAllLines(quizPath, lines);
    Console.WriteLine($"  -> {quizPath} ({items.Count} items)");
}

Console.WriteLine("\nDone!");

static string EscapeDescription(string? desc)
{
    return desc?.Replace("\\", "\\\\").Replace("\"", "\\\"") ?? "";
}

class QuizItem
{
    public string Name { get; set; } = "";
    public List<CategoryEntry> Categories { get; set; } = [];
}

class CategoryEntry
{
    public string Category { get; set; } = "";
    public int Result { get; set; }
    public string Description { get; set; } = "";
}
