namespace IsIt.Models;

public class QuizItem
{
    public string Name { get; set; } = string.Empty;
    public List<string> Categories { get; set; } = [];
    public string Explanation { get; set; } = string.Empty;
}
