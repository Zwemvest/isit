namespace IsIt.Models;

public class DailyResult
{
    public string Date { get; set; } = "";
    public int Score { get; set; }
    public int TotalQuestions { get; set; }
    public List<string> Categories { get; set; } = [];
    public List<SavedAnswer> Answers { get; set; } = [];
}

public class SavedAnswer
{
    public string ItemName { get; set; } = "";
    public List<string> CorrectCategories { get; set; } = [];
    public List<string> SelectedCategories { get; set; } = [];
    public bool WasCorrect { get; set; }
    public string Explanation { get; set; } = "";
}

public class DailyHistoryEntry
{
    public string Date { get; set; } = "";
    public int Score { get; set; }
    public int TotalQuestions { get; set; }
}
