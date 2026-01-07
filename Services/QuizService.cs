using System.Net.Http.Json;
using IsIt.Models;

namespace IsIt.Services;

public enum ScoringMode
{
    AllCorrect,
    AnyCorrect
}

public class QuizService
{
    private readonly HttpClient _httpClient;
    private List<QuizItem> _items = [];
    private List<QuizItem> _shuffledItems = [];
    private int _currentIndex;
    private int _score;

    // Settings
    public ScoringMode ScoringMode { get; set; } = ScoringMode.AllCorrect;
    public HashSet<string> IncludedCategories { get; private set; } = [];

    public const int MinCategories = 3;

    public static readonly List<string> AllCategories =
    [
        "LordOfTheRings",
        "Pokemon",
        "Tech",
        "Psychiatric",
        "PaganGod",
        "MetalBand",
        "IKEAFurniture",
        "StarWars",
        "Programming",
        "HistoricalState",
        "DnD"
    ];

    public static readonly Dictionary<string, string> CategoryDisplayNames = new()
    {
        ["LordOfTheRings"] = "Lord of the Rings",
        ["Pokemon"] = "Pokémon",
        ["Tech"] = "Tech",
        ["Psychiatric"] = "Psychiatric Medication",
        ["PaganGod"] = "Pagan God",
        ["MetalBand"] = "Metal Band",
        ["IKEAFurniture"] = "IKEA Furniture",
        ["StarWars"] = "Star Wars",
        ["Programming"] = "Programming Term",
        ["HistoricalState"] = "Historical State or City",
        ["DnD"] = "Dungeons and Dragons"
    };

    public QuizService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        InitializeRandomCategories();
    }

    public void InitializeRandomCategories()
    {
        // Default: 6 random categories
        IncludedCategories = AllCategories
            .OrderBy(_ => Random.Shared.Next())
            .Take(6)
            .ToHashSet();
    }

    public void SetIncludedCategories(IEnumerable<string> categories)
    {
        var validCategories = categories.Where(c => AllCategories.Contains(c)).ToHashSet();
        if (validCategories.Count >= MinCategories)
        {
            IncludedCategories = validCategories;
        }
    }

    public async Task LoadItemsAsync()
    {
        if (_items.Count == 0)
        {
            _items = await _httpClient.GetFromJsonAsync<List<QuizItem>>("data/quiz-items.json") ?? [];
        }
    }

    private const int QuestionsPerGame = 20;

    public void StartNewGame()
    {
        // Filter items to those that have at least one included category
        _shuffledItems = _items
            .Where(item => item.Categories.Any(c => IncludedCategories.Contains(c)))
            .OrderBy(_ => Random.Shared.Next())
            .Take(QuestionsPerGame)
            .ToList();
        _currentIndex = 0;
        _score = 0;
    }

    /// <summary>
    /// Gets the categories for the current item that are included in the game.
    /// Excluded categories are filtered out.
    /// </summary>
    public List<string> GetCurrentItemIncludedCategories()
    {
        var current = GetCurrentItem();
        if (current == null) return [];
        return current.Categories.Where(c => IncludedCategories.Contains(c)).ToList();
    }

    public QuizItem? GetCurrentItem()
    {
        if (_currentIndex >= 0 && _currentIndex < _shuffledItems.Count)
        {
            return _shuffledItems[_currentIndex];
        }
        return null;
    }

    public int CurrentQuestionNumber => _currentIndex + 1;
    public int TotalQuestions => _shuffledItems.Count;
    public int Score => _score;
    public bool IsFinished => _currentIndex >= _shuffledItems.Count;

    public bool CheckAnswer(IEnumerable<string> selectedCategories)
    {
        var current = GetCurrentItem();
        if (current == null) return false;

        var selected = selectedCategories.ToHashSet();
        // Only consider included categories as "correct" - excluded ones don't count
        var correctIncluded = current.Categories.Where(c => IncludedCategories.Contains(c)).ToHashSet();

        bool isCorrect;
        if (ScoringMode == ScoringMode.AllCorrect)
        {
            // Must match exactly all included categories
            isCorrect = selected.SetEquals(correctIncluded);
        }
        else // AnyCorrect
        {
            // At least one selected category must be correct
            isCorrect = selected.Any(s => correctIncluded.Contains(s));
        }

        if (isCorrect)
        {
            _score++;
        }

        return isCorrect;
    }

    public void MoveToNext()
    {
        _currentIndex++;
    }
}
