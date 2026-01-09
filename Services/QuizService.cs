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

public record AnswerRecord(QuizItem Item, HashSet<string> SelectedCategories, HashSet<string> CorrectCategories, bool WasCorrect, HashSet<string> MissedCategories);

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
        "MetalMusic",
        "RockMusic",
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
        "CarModel",
        "CloudInfra",
        "MTG",
        "Celestial",
        "Bird",
        "Pigment",
        "SmashBros",
        "AssassinsCreed",
        "Superhero",
        "GreekLetter",
        "SpaceMission"
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
        ["MetalMusic"] = "Metal Music",
        ["RockMusic"] = "Rock Music",
        ["IKEAFurniture"] = "IKEA Furniture",
        ["StarWars"] = "Star Wars",
        ["ProgrammingLang"] = "Programming Language or Framework",
        ["DigitalTerm"] = "Digital Terminology",
        ["SiliconValleyBS"] = "Silicon Valley Venture Capitalist Bullshit",
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
        ["CarModel"] = "Car Model",
        ["CloudInfra"] = "Cloud Infrastructure Tool",
        ["MTG"] = "Magic: The Gathering",
        ["Celestial"] = "Celestial Body",
        ["Bird"] = "Ornithology",
        ["Pigment"] = "Colours or Pigments",
        ["SmashBros"] = "Super Smash Bros. Character",
        ["AssassinsCreed"] = "Assassin's Creed Appearance",
        ["Superhero"] = "Marvel/DC Superhero",
        ["GreekLetter"] = "Greek Letter",
        ["SpaceMission"] = "NASA Space Mission"
    };

    public QuizService(HttpClient httpClient)
    {
        _httpClient = httpClient;
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
        "data/quiz-items/mythology.quiz",
        "data/quiz-items/fantasy.quiz",
        "data/quiz-items/anime-games.quiz",
        "data/quiz-items/music.quiz",
        "data/quiz-items/tech.quiz",
        "data/quiz-items/people.quiz",
        "data/quiz-items/things.quiz"
    ];

    public async Task LoadItemsAsync()
    {
        if (_items.Count == 0)
        {
            foreach (var file in DataFiles)
            {
                var content = await _httpClient.GetStringAsync(file);
                var items = QuizDataParser.Parse(content);
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
            .Where(item => item.Categories.Any(c => IncludedCategories.Contains(c.Category) && c.Result == ResultType.Correct))
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
            .Where(item => item.Categories.Any(c => IncludedCategories.Contains(c.Category) && c.Result == ResultType.Correct))
            .OrderBy(_ => rng.Next())
            .Take(DailyQuestions)
            .ToList();

        _currentIndex = 0;
        _score = 0;
        _answerHistory = [];
    }

    public bool IsDailyComplete => CurrentGameMode == GameMode.Daily && _currentIndex >= DailyQuestions;

    public void RestoreDailyGame(int currentIndex, int score)
    {
        // Start a fresh daily game (this sets up categories and shuffled items deterministically)
        StartDailyGame();

        // Then restore the saved progress
        _currentIndex = currentIndex;
        _score = score;
        // Note: _answerHistory is not restored - the saved answers are stored in DailyResult
    }

    /// <summary>
    /// Gets the category descriptions for a quiz item.
    /// </summary>
    public Dictionary<string, string> GetCategoryDescriptions(QuizItem item)
    {
        return item.Categories
            .Where(c => IncludedCategories.Contains(c.Category))
            .ToDictionary(c => c.Category, c => c.Description);
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
    public int Score => _score;
    public bool IsFinished => _currentIndex >= _shuffledItems.Count;
    public bool HasReachedMinQuestions => _currentIndex >= QuestionsPerGame;
    public const int MinQuestionsForResults = 20;

    public bool CheckAnswer(IEnumerable<string> selectedCategories)
    {
        var current = GetCurrentItem();
        if (current == null) return false;

        var selected = selectedCategories.ToHashSet();

        // Get categories by result type (only included ones)
        var correctCategories = current.Categories
            .Where(c => IncludedCategories.Contains(c.Category) && c.Result == ResultType.Correct)
            .Select(c => c.Category)
            .ToHashSet();

        // Arguable and Obscure are both treated as "acceptable" selections
        var arguableCategories = current.Categories
            .Where(c => IncludedCategories.Contains(c.Category) && c.Result == ResultType.Arguable)
            .Select(c => c.Category)
            .ToHashSet();

        var obscureCategories = current.Categories
            .Where(c => IncludedCategories.Contains(c.Category) && c.Result == ResultType.Obscure)
            .Select(c => c.Category)
            .ToHashSet();

        var missCategories = current.Categories
            .Where(c => IncludedCategories.Contains(c.Category) && c.Result == ResultType.Miss)
            .Select(c => c.Category)
            .ToHashSet();

        // Valid categories are Correct, Arguable, or Obscure - selecting anything else is wrong
        var validSelectableCategories = correctCategories.Union(arguableCategories).Union(obscureCategories).ToHashSet();

        // Check if ALL selected categories are valid (no erroneous selections)
        var hasOnlyValidSelections = selected.All(s => validSelectableCategories.Contains(s));

        bool isCorrect;
        if (ScoringMode == ScoringMode.AllCorrect)
        {
            // Must select ALL "Correct" categories AND only valid categories (no erroneous ones)
            var hasAllCorrect = correctCategories.All(c => selected.Contains(c));
            // Arguable/Obscure categories are optional and don't affect scoring

            isCorrect = hasAllCorrect && hasOnlyValidSelections;
        }
        else // AnyCorrect
        {
            // At least one "Correct", "Arguable", or "Obscure" category must be selected
            // AND must not have any erroneous selections
            var hasAnyValid = selected.Any(s => validSelectableCategories.Contains(s));

            isCorrect = hasAnyValid && hasOnlyValidSelections;
        }

        if (isCorrect)
        {
            _score++;
        }

        // For display purposes, combine Correct, Arguable, and Obscure as "acceptable answers"
        var allCorrectForDisplay = correctCategories.Union(arguableCategories).Union(obscureCategories).ToHashSet();

        // Track which Miss categories were incorrectly selected
        var selectedMissCategories = selected.Where(s => missCategories.Contains(s)).ToHashSet();

        // Record the answer
        _answerHistory.Add(new AnswerRecord(current, selected, allCorrectForDisplay, isCorrect, selectedMissCategories));

        return isCorrect;
    }

    public IReadOnlyList<AnswerRecord> AnswerHistory => _answerHistory;

    public void MoveToNext()
    {
        _currentIndex++;
    }
}
