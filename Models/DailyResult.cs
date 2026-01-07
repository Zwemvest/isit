namespace IsIt.Models;

public class DailyResult
{
    public string Date { get; set; } = "";
    public int Score { get; set; }
    public int TotalQuestions { get; set; }
    public List<string> Categories { get; set; } = [];
}
