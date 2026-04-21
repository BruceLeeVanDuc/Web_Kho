namespace OlapWebBTL.Models;

/// <summary>
/// Danh sách giá trị các dropdown filter (lấy từ dim tables).
/// </summary>
public class FilterOptions
{
    public List<int> Years { get; set; } = new();
    public List<int> Quarters { get; } = new() { 1, 2, 3, 4 };
    public List<int> Months { get; } = Enumerable.Range(1, 12).ToList();
    public List<string> Bangs { get; set; } = new();
    public List<string> Cities { get; set; } = new();
    public List<string> CustomerTypes { get; set; } = new();
    public Dictionary<string, List<string>> CitiesByBang { get; set; } = new();
}
