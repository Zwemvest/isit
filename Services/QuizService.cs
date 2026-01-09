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
        "LOTR",
        "Pokemon",
        "Tech",
        "Psychiatric",
        "NorsePagan",
        "GreekPagan",
        "RomanPagan",
        "CelticPagan",
        "MetalMusic",
        "RockMusic",
        "IKEA",
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
        ["LOTR"] = "Tolkien's Lord of the Rings",
        ["Pokemon"] = "Pokémon",
        ["Tech"] = "Tech Company or Product",
        ["Psychiatric"] = "Psychiatric Medication",
        ["NorsePagan"] = "Norse Mythology",
        ["GreekPagan"] = "Greek Mythology",
        ["RomanPagan"] = "Roman Mythology",
        ["CelticPagan"] = "Celtic Mythology",
        ["MetalMusic"] = "Metal Music",
        ["RockMusic"] = "Rock Music",
        ["IKEA"] = "IKEA Furniture",
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

    public static readonly Dictionary<string, string> CategoryTooltips = new()
    {
        ["LOTR"] = "Characters, places, and items from Tolkien's Middle-earth",
        ["Pokemon"] = "Everything Pokémon. Includes species names, moves, items, locations, cities, and more.",
        ["Tech"] = "Technology companies, products, services, or brands",
        ["Psychiatric"] = "Prescription psychiatric medications and drug names",
        ["NorsePagan"] = "Gods, creatures, and concepts from Norse/Viking mythology",
        ["GreekPagan"] = "Gods, heroes, and creatures from ancient Greek mythology",
        ["RomanPagan"] = "Gods and figures from ancient Roman mythology",
        ["CelticPagan"] = "Gods, heroes, and creatures from Celtic/Irish mythology",
        ["MetalMusic"] = "Metal bands, albums, or songs",
        ["RockMusic"] = "Rock bands, albums, or songs",
        ["IKEA"] = "IKEA product names",
        ["StarWars"] = "Characters, ships, planets from the Star Wars universe",
        ["ProgrammingLang"] = "Programming languages, frameworks, or development tools",
        ["DigitalTerm"] = "Technical terminology used in computing and digital media",
        ["SiliconValleyBS"] = "Everything crypto, blockchain, NFT, or other nonsensical bullshit that only exists to please venture capitalists.",
        ["HistoricalState"] = "Names of historical nations, empires, city-states, or ancient cities",
        ["DnD"] = "Monsters, creatures, Gods, spells, classes, or items from Dungeons & Dragons or Forgotten Realms or Eberron Lore",
        ["Warhammer"] = "Factions, units, or characters from Warhammer 40K/Fantasy",
        ["Zelda"] = "Characters, items, or places from The Legend of Zelda series",
        ["Yugioh"] = "Cards or characters from Yu-Gi-Oh!",
        ["Digimon"] = "Digimon species names",
        ["DragonBallZ"] = "Characters or techniques from Dragon Ball",
        ["JoJo"] = "Characters, Stands, or references from JoJo's Bizarre Adventure",
        ["Painter"] = "Famous painters throughout history",
        ["Philosopher"] = "Notable philosophers throughout history",
        ["Author"] = "Famous authors and writers. (if the philosopher category is included, nearly all philosophers will also be authors).",
        ["CarModel"] = "Car model names (not brands)",
        ["CloudInfra"] = "Cloud platforms, infrastructure tools, and DevOps software",
        ["MTG"] = "Cards, mechanics, or planes from Magic: The Gathering",
        ["Celestial"] = "Natural objects in space: planets, moons, stars, asteroids (not zodiac signs)",
        ["Bird"] = "Bird species names",
        ["Pigment"] = "Color names, pigments, or dyes",
        ["SmashBros"] = "Playable fighters in Super Smash Bros. games",
        ["AssassinsCreed"] = "Historical figures or settings featured in Assassin's Creed",
        ["Superhero"] = "Superheroes or villains from Marvel or DC comics",
        ["GreekLetter"] = "Letters of the Greek alphabet",
        ["SpaceMission"] = "NASA missions or space programs (does not include spacecraft)"
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
