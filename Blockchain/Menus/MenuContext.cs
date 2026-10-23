using Blockchain.Models.Metrics;

namespace Blockchain.Menus;

public class MenuContext
{
    private const int MaxStatusMessages = 5;
    private readonly Queue<(string Message, bool Success)> _statusMessages = new();
    public IReadOnlyCollection<(string Message, bool Success)> StatusMessages
        => _statusMessages.ToArray();

    public bool ShowMetrics { get; private set; }

    public MiningMetrics? LastMiningMetrics { get; set; }
    public ChainRepairMetrics? LastRepairMetrics { get; set; }
    public DifficultyChangeMetrics? ChangeMetrics { get; set; }
    public NodeStatistics NodeStatistics { get; set; }

    public MenuContext(bool showMetrics = false)
    {
        ShowMetrics = showMetrics;
    }
    public void SetStatus(string message, bool success = true)
    {
        if (_statusMessages.Count >= MaxStatusMessages)
            _statusMessages.Dequeue();

        _statusMessages.Enqueue((message, success));
    }
    public void ClearStatus()
    {
        _statusMessages.Clear();
    }
    public void ToggleMetrics()
    {
        ShowMetrics = !ShowMetrics;
        SetStatus($"Mining metrics {(ShowMetrics ? "enabled" : "disabled")}.");
    }
}