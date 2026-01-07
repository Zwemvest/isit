using System.Net.Http.Json;
using IsIt.Models;

namespace IsIt.Services;

public enum ScoringMode
{
    AllCorrect,
    AnyCorrect
}

public enum GameMode
{
    Daily,
    Custom
}

public record AnswerRecord(QuizItem Item, HashSet<string> SelectedCategories, HashSet<string> CorrectCategories, bool WasCorrect);

public class QuizService
{
    private readonly HttpClient _httpClient;
    private List<QuizItem> _items = [];
    private List<QuizItem> _shuffledItems = [];
    private int _currentIndex;
    private int _score;
    private List<AnswerRecord> _answerHistory = [];

    // Settings
    public ScoringMode ScoringMode { get; set; } = ScoringMode.AllCorrect;
    public HashSet<string> IncludedCategories { get; private set; } = [];
    public GameMode CurrentGameMode { get; private set; } = GameMode.Custom;

    public const int MinCategories = 3;
    public const int DailyCategories = 6;
    public const int DailyQuestions = 20;

    public static readonly List<string> AllCategories =
    [
        "LordOfTheRings",
        "Pokemon",
        "Tech",
        "Psychiatric",
        "NorsePagan",
        "GreekPagan",
        "RomanPagan",
        "CelticPagan",
        "MetalBand",
        "RockBand",
        "IKEAFurniture",
        "StarWars",
        "ProgrammingLang",
        "DigitalTerm",
        "SiliconValleyBS",
        "HistoricalState",
        "DnD",
        "Warhammer",
        "Zelda",
        "Yugioh",
        "Digimon",
        "DragonBallZ",
        "JoJo",
        "Painter",
        "Philosopher",
        "Author",
        "CarModel"
    ];

    public static readonly Dictionary<string, string> CategoryDisplayNames = new()
    {
        ["LordOfTheRings"] = "Lord of the Rings",
        ["Pokemon"] = "Pokémon",
        ["Tech"] = "Tech Company or Product",
        ["Psychiatric"] = "Psychiatric Medication",
        ["NorsePagan"] = "Norse Mythology",
        ["GreekPagan"] = "Greek Mythology",
        ["RomanPagan"] = "Roman Mythology",
        ["CelticPagan"] = "Celtic Mythology",
        ["MetalBand"] = "Metal Band",
        ["RockBand"] = "Rock Band",
        ["IKEAFurniture"] = "IKEA Furniture",
        ["StarWars"] = "Star Wars",
        ["ProgrammingLang"] = "Programming Language or Framework",
        ["DigitalTerm"] = "Digital Terminology",
        ["SiliconValleyBS"] = "Silicon Valley VC Bullshit",
        ["HistoricalState"] = "Historical State or City",
        ["DnD"] = "Dungeons and Dragons",
        ["Warhammer"] = "Warhammer",
        ["Zelda"] = "Legend of Zelda",
        ["Yugioh"] = "Yu-Gi-Oh!",
        ["Digimon"] = "Digimon",
        ["DragonBallZ"] = "Dragon Ball Z",
        ["JoJo"] = "JoJo's Bizarre Adventure",
        ["Painter"] = "Famous Painter",
        ["Philosopher"] = "Philosopher",
        ["Author"] = "Famous Author",
        ["CarModel"] = "Car Model"
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

    private static readonly string[] DataFiles =
    [
        "data/quiz-items/mythology.json",
        "data/quiz-items/fantasy.json",
        "data/quiz-items/anime-games.json",
        "data/quiz-items/music.json",
        "data/quiz-items/tech.json",
        "data/quiz-items/people.json",
        "data/quiz-items/things.json"
    ];

    public async Task LoadItemsAsync()
    {
        if (_items.Count == 0)
        {
            foreach (var file in DataFiles)
            {
                var items = await _httpClient.GetFromJsonAsync<List<QuizItem>>(file) ?? [];
                _items.AddRange(items);
            }
        }
    }

    private const int QuestionsPerGame = 20;

    public void StartNewGame()
    {
        CurrentGameMode = GameMode.Custom;

        // Filter items to those that have at least one included category
        // In Custom/endless mode, we take ALL matching items (no limit)
        _shuffledItems = _items
            .Where(item => item.Categories.Any(c => IncludedCategories.Contains(c)))
            .OrderBy(_ => Random.Shared.Next())
            .ToList();
        _currentIndex = 0;
        _score = 0;
        _answerHistory = [];
    }

    public int GetDailySeed()
    {
        var today = DateTime.UtcNow.Date;
        return today.Year * 10000 + today.Month * 100 + today.Day;
    }

    public string GetDailyDateString()
    {
        return DateTime.UtcNow.Date.ToString("yyyy-MM-dd");
    }

    public void StartDailyGame()
    {
        CurrentGameMode = GameMode.Daily;
        var seed = GetDailySeed();
        var rng = new Random(seed);

        // Select 6 categories using the seeded RNG
        IncludedCategories = AllCategories
            .OrderBy(_ => rng.Next())
            .Take(DailyCategories)
            .ToHashSet();

        // Filter and shuffle items using the same seed
        _shuffledItems = _items
            .Where(item => item.Categories.Any(c => IncludedCategories.Contains(c)))
            .OrderBy(_ => rng.Next())
            .Take(DailyQuestions)
            .ToList();

        _currentIndex = 0;
        _score = 0;
        _answerHistory = [];
    }

    public bool IsDailyComplete => CurrentGameMode == GameMode.Daily && _currentIndex >= DailyQuestions;

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
    public bool HasReachedMinQuestions => _currentIndex >= QuestionsPerGame;
    public const int MinQuestionsForResults = 20;

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

        // Record the answer
        _answerHistory.Add(new AnswerRecord(current, selected, correctIncluded, isCorrect));

        return isCorrect;
    }

    public IReadOnlyList<AnswerRecord> AnswerHistory => _answerHistory;

    public void MoveToNext()
    {
        _currentIndex++;
    }
}
