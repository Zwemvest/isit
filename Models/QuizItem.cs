namespace IsIt.Models;

public enum ResultType
{
    Miss = -1,
    Correct = 0,
    Arguable = 1,
    Obscure = 2
}

public class CategoryEntry
{
    public string Category { get; set; } = string.Empty;
    public ResultType Result { get; set; } = ResultType.Correct;
    public string Description { get; set; } = string.Empty;
}

public class QuizItem
{
    public string Name { get; set; } = string.Empty;
    public List<CategoryEntry> Categories { get; set; } = [];
}
