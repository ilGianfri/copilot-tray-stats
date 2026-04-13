namespace CopilotTrayStats.ViewModels;

public class ChartBarViewModel
{
    public DateOnly Date { get; init; }
    public int Used { get; init; }
    public double BarHeight { get; init; }
    public string Tooltip { get; init; } = "";
    public bool IsToday { get; init; }
    public string DateLabel => Date.ToString("dd\'/'MM");
}
