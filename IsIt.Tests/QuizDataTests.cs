using IsIt.Services;

namespace IsIt.Tests;

public class QuizDataTests
{
    private static readonly string[] DataFiles =
    [
        "mythology.quiz",
        "fantasy.quiz",
        "anime-games.quiz",
        "music.quiz",
        "tech.quiz",
        "people.quiz",
        "things.quiz"
    ];

    private static string GetQuizDataPath()
    {
        // Navigate up from bin/Debug/net8.0 to find wwwroot
        var dir = AppContext.BaseDirectory;
        while (dir != null && !Directory.Exists(Path.Combine(dir, "wwwroot")))
        {
            dir = Directory.GetParent(dir)?.FullName;
        }

        return dir != null
            ? Path.Combine(dir, "wwwroot", "data", "quiz-items")
            : throw new DirectoryNotFoundException("Could not find wwwroot directory");
    }

    [Fact]
    public void AllEntries_ShouldHaveUniqueNames()
    {
        var quizDataPath = GetQuizDataPath();
        var allNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var duplicates = new List<(string Name, string File1, string File2)>();

        foreach (var file in DataFiles)
        {
            var filePath = Path.Combine(quizDataPath, file);
            var content = File.ReadAllText(filePath);
            var items = QuizDataParser.Parse(content);

            foreach (var item in items)
            {
                if (!allNames.Add(item.Name))
                {
                    // Find which file had it first
                    var firstFile = FindFileContaining(quizDataPath, item.Name, file);
                    duplicates.Add((item.Name, firstFile, file));
                }
            }
        }

        Assert.True(duplicates.Count == 0,
            $"Found {duplicates.Count} duplicate entries:\n" +
            string.Join("\n", duplicates.Select(d => $"  '{d.Name}' in both {d.File1} and {d.File2}")));
    }

    private static string FindFileContaining(string quizDataPath, string name, string excludeFile)
    {
        foreach (var file in DataFiles)
        {
            if (file == excludeFile) continue;

            var filePath = Path.Combine(quizDataPath, file);
            var content = File.ReadAllText(filePath);
            var items = QuizDataParser.Parse(content);

            if (items.Any(i => i.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            {
                return file;
            }
        }
        return "unknown";
    }

    [Fact]
    public void AllEntries_ShouldHaveAtLeastOneCategory()
    {
        var quizDataPath = GetQuizDataPath();
        var entriesWithNoCategories = new List<(string Name, string File)>();

        foreach (var file in DataFiles)
        {
            var filePath = Path.Combine(quizDataPath, file);
            var content = File.ReadAllText(filePath);
            var items = QuizDataParser.Parse(content);

            foreach (var item in items)
            {
                if (item.Categories.Count == 0)
                {
                    entriesWithNoCategories.Add((item.Name, file));
                }
            }
        }

        Assert.True(entriesWithNoCategories.Count == 0,
            $"Found {entriesWithNoCategories.Count} entries with no categories:\n" +
            string.Join("\n", entriesWithNoCategories.Select(e => $"  '{e.Name}' in {e.File}")));
    }

    [Fact]
    public void AllEntries_ShouldUseValidCategories()
    {
        var quizDataPath = GetQuizDataPath();
        var invalidCategories = new List<(string Name, string Category, string File)>();

        foreach (var file in DataFiles)
        {
            var filePath = Path.Combine(quizDataPath, file);
            var content = File.ReadAllText(filePath);
            var items = QuizDataParser.Parse(content);

            foreach (var item in items)
            {
                foreach (var cat in item.Categories)
                {
                    if (!QuizService.AllCategories.Contains(cat.Category))
                    {
                        invalidCategories.Add((item.Name, cat.Category, file));
                    }
                }
            }
        }

        Assert.True(invalidCategories.Count == 0,
            $"Found {invalidCategories.Count} entries with invalid categories:\n" +
            string.Join("\n", invalidCategories.Select(e => $"  '{e.Name}' uses unknown category '{e.Category}' in {e.File}")));
    }
}
