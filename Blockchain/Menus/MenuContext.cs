using Blockchain.Models.Metrics;

namespace Blockchain.Menus;

public class MenuContext
{
    public string? StatusMessage { get; private set; }
    public bool StatusSuccess { get; private set; } = true;

    public bool ShowMetrics { get; private set; }

    public MiningMetrics? LastMiningMetrics { get; set; }
    public ChainRepairMetrics? LastRepairMetrics { get; set; }
    public DifficultyChangeMetrics? ChangeMetrics { get; set; }

    public MenuContext(bool showMetrics = false)
    {
        ShowMetrics = showMetrics;
    }
    public void SetStatus(string message, bool success = true)
    {
        StatusMessage = message;
        StatusSuccess = success;
    }
    public void ClearStatus()
    {
        StatusMessage = null;
        StatusSuccess = true;
    }
    public void ToggleMetrics()
    {
        ShowMetrics = !ShowMetrics;
        SetStatus($"Mining metrics {(ShowMetrics ? "enabled" : "disabled")}.");
    }
}