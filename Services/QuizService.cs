using System.Net.Http.Json;
using IsIt.Models;

namespace IsIt.Services;

public class QuizService
{
    private readonly HttpClient _httpClient;
    private List<QuizItem> _items = [];
    private List<QuizItem> _shuffledItems = [];
    private int _currentIndex;
    private int _score;

    public static readonly List<string> AllCategories =
    [
        "LordOfTheRings",
        "Pokemon",
        "Tech",
        "Antidepressant",
        "PaganGod",
        "MetalBand",
        "IKEAFurniture",
        "StarWars"
    ];

    public static readonly Dictionary<string, string> CategoryDisplayNames = new()
    {
        ["LordOfTheRings"] = "Lord of the Rings",
        ["Pokemon"] = "Pokémon",
        ["Tech"] = "Tech",
        ["Antidepressant"] = "Antidepressant",
        ["PaganGod"] = "Pagan God",
        ["MetalBand"] = "Metal Band",
        ["IKEAFurniture"] = "IKEA Furniture",
        ["StarWars"] = "Star Wars"
    };

    public QuizService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task LoadItemsAsync()
    {
        if (_items.Count == 0)
        {
            _items = await _httpClient.GetFromJsonAsync<List<QuizItem>>("data/quiz-items.json") ?? [];
        }
    }

    public void StartNewGame()
    {
        _shuffledItems = _items.OrderBy(_ => Random.Shared.Next()).ToList();
        _currentIndex = 0;
        _score = 0;
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

        var selected = selectedCategories.OrderBy(x => x).ToList();
        var correct = current.Categories.OrderBy(x => x).ToList();

        var isCorrect = selected.SequenceEqual(correct);
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
