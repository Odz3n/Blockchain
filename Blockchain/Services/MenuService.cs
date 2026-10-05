using Blockchain.Models;
using Blockchain.Models.Metrics;

namespace Blockchain.Services;

public class MenuService
{
    private readonly BlockchainService _blockchainService;
    private readonly DisplayService _displayService;

    private string? _statusMessage;
    private bool _statusSuccess = true;
    private MiningMetrics? _lastMiningMetrics;
    private ChainRepairMetrics? _lastRepairMetrics;
    private DifficultyChangeMetrics? _changeMetrics;

    public bool ShowMetrics { get; private set; }

    public MenuService(int initialDifficulty = 2, bool showMetrics = false)
    {
        ShowMetrics = showMetrics;
        _blockchainService = new BlockchainService(initialDifficulty);
        _displayService = new DisplayService();
    }
    public void Run()
    {
        while (true)
        {
            Console.Clear();
            ShowMenuBar();

            if (!int.TryParse(Console.ReadLine(), out int actionId))
            {
                ShowMessage("Invalid menu option.", false);
                continue;
            }

            if (actionId == 0)
                return;

            Action(actionId);
        }
    }
    private void ShowMenuBar()
    {
        PrintHeader("BLOCKCHAIN MANAGER");
        ShowStatusBar();

        Console.WriteLine();

        PrintInfo("Difficulty", _blockchainService.Difficulty.ToString());
        PrintInfo("Mask", _blockchainService.HashMask);
        PrintInfo("Blocks", _blockchainService.Chain.Count.ToString());
        PrintInfo("Metrics", ShowMetrics ? "ON" : "OFF");

        if (ShowMetrics)
        {
            if (_lastRepairMetrics != null)
                _displayService.DisplayRepairMetrics(_lastRepairMetrics);
            if (_lastMiningMetrics != null)
                _displayService.DisplayMiningMetrics(_lastMiningMetrics);
            if (_changeMetrics != null)
                _displayService.DisplayDifficultyChangeMetrics(_changeMetrics);
        }

        PrintSeparator();

        PrintOption(1, "Change Difficulty");
        PrintOption(2, "Add Block");
        PrintOption(3, "Show Chain");
        PrintOption(4, "Change Hash Mask");
        PrintOption(5, "Change Data");
        PrintOption(6, "Validate");
        PrintOption(7, "Re-mine Block");
        PrintOption(8, "Repair Chain");
        PrintOption(9, "Toggle Metrics");
        PrintOption(10, "Show Chain metrics");
        PrintOption(0, "Exit");

        Console.Write("\n  >>> ");
    }
    private void Action(int actionId)
    {
        switch (actionId)
        {
            case 1:
                ChangeDifficulty();
                break;
            case 2:
                AddBlock();
                break;
            case 3:
                ShowChain();
                break;
            case 4:
                ChangeHashMask();
                break;
            case 5:
                ChangeData();
                break;
            case 6:
                Validate();
                break;
            case 7:
                Remine();
                break;
            case 8:
                Fix();
                break;
            case 9:
                ToggleMetrics();
                break;
            case 10:
                ShowChainMetrics();
                break;
            default:
                ShowMessage("Unknown menu option.", false);
                break;
        }
    }

    private void ShowChainMetrics()
    {
        Console.Clear();
        PrintHeader("CHAIN METRICS");

        var res = _blockchainService.GetChainMetrics();

        PrintInfo("Fastest block", res.FastestBlock.Index.ToString());
        _displayService.DisplayBlock(res.FastestBlock);

        PrintInfo("Slowest block", res.SlowestBlock.Index.ToString());
        _displayService.DisplayBlock(res.SlowestBlock);

        PrintInfo("Most attempts block", res.MostAttemptsBlock.Index.ToString());
        _displayService.DisplayBlock(res.MostAttemptsBlock);

        PrintInfo("Average mining time", res.AvgMiningTime.ToString());
        PrintInfo("Average attempts count", res.AvgAttemptsCount.ToString());
        PrintInfo("Global min difficulty", res.MinDiffAtMining.ToString());
        PrintInfo("Global max difficulty", res.MaxDiffAtMining.ToString());

        WaitForKey();
    }

    private void ChangeDifficulty()
    {
        Console.Clear();
        PrintHeader("CHANGE DIFFICULTY");
        PrintInfo("Current", _blockchainService.Difficulty.ToString());

        WriteColored("\n  New difficulty: ", ConsoleColor.Cyan);

        if (!int.TryParse(Console.ReadLine(), out int difficulty))
        {
            ShowMessage("Invalid difficulty.", false);
            return;
        }

        try
        {
            _blockchainService.ChangeDifficulty(difficulty);
            ShowMessage($"Difficulty changed to {difficulty}.");
        }
        catch (ArgumentException ex)
        {
            ShowMessage(ex.Message, false);
        }
    }
    private void ChangeHashMask()
    {
        Console.Clear();
        PrintHeader("CHANGE HASH MASK");
        PrintInfo("Current", _blockchainService.HashMask);

        WriteColored("\n  New mask: ", ConsoleColor.Cyan);

        string? mask = Console.ReadLine();

        try
        {
            _blockchainService.ChangeHashMask(mask ?? "");
            ShowMessage($"Hash mask changed to {_blockchainService.HashMask}.");
        }
        catch (ArgumentException ex)
        {
            ShowMessage(ex.Message, false);
        }
    }
    private void AddBlock()
    {
        Console.Clear();
        PrintHeader("ADD BLOCK");

        WriteColored("\n  Block data: ", ConsoleColor.Cyan);

        string? data = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(data))
        {
            ShowMessage("Block data cannot be empty.", false);
            return;
        }

        try
        {
            PrintStatus("Mining block...", ConsoleColor.Yellow);

            var metrics = _blockchainService.AddBlock(data, "N/A");
            if (metrics != null)
                _changeMetrics = metrics;

            var block = _blockchainService.Chain[^1];

            ShowMessage($"Block #{block.Index} mined successfully.");
        }
        catch (Exception ex)
        {
            ShowMessage(ex.Message, false);
        }
    }
    private void ShowChain()
    {
        Console.Clear();
        _displayService.Display(_blockchainService.Chain);
        WaitForKey();
    }
    private void ChangeData()
    {
        Console.Clear();
        PrintHeader("CHANGE BLOCK DATA");

        var block = SelectBlock(false);

        if (block == null)
        {
            ShowMessage("Invalid block selection.", false);
            return;
        }

        PrintInfo("Current Data", block.Data);
        WriteColored("\n  New data: ", ConsoleColor.Cyan);

        string? data = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(data))
        {
            ShowMessage("Block data cannot be empty.", false);
            return;
        }

        try
        {
            PrintStatus("Updating block...", ConsoleColor.Yellow);

            _blockchainService.ChangeData(block, data);

            ShowMessage($"Block #{block.Index} updated. Chain may require repair.");
        }
        catch (Exception ex)
        {
            ShowMessage(ex.Message, false);
        }
    }
    private void Validate()
    {
        Console.Clear();
        PrintHeader("VALIDATE CHAIN");

        try
        {
            var res = _blockchainService.IsValid();

            ShowMessage(res.Message, res.IsValid);
        }
        catch (Exception ex)
        {
            ShowMessage(ex.Message, false);
        }
    }
    private void Remine()
    {
        Console.Clear();
        PrintHeader("RE-MINE BLOCK");

        var block = SelectBlock();

        if (block == null)
        {
            ShowMessage("Invalid block selection.", false);
            return;
        }

        try
        {
            PrintStatus("Re-mining block...", ConsoleColor.Yellow);

            _lastMiningMetrics = _blockchainService.RemineBlock(block);
            _lastRepairMetrics = null;

            ShowMessage($"Block #{block.Index} re-mined successfully.");
        }
        catch (Exception ex)
        {
            ShowMessage(ex.Message, false);
        }
    }
    private void Fix()
    {
        Console.Clear();
        PrintHeader("REPAIR CHAIN");

        WriteColored("\n  Start index (0 for genesis): ", ConsoleColor.Cyan);

        if (!int.TryParse(Console.ReadLine(), out int startIndex) || startIndex < 0 || startIndex >= _blockchainService.Chain.Count)
        {
            ShowMessage("Invalid starting index.", false);
            return;
        }

        try
        {
            PrintStatus("Repairing chain...", ConsoleColor.Yellow);

            _lastRepairMetrics = _blockchainService.RepairChain(startIndex);
            _lastMiningMetrics = null;

            var res = _blockchainService.IsValid();

            ShowMessage(res.IsValid ? "Chain repaired successfully." : "Repair completed, but validation failed.", res.IsValid);
        }
        catch (Exception ex)
        {
            ShowMessage(ex.Message, false);
        }
    }
    private void ToggleMetrics()
    {
        ShowMetrics = !ShowMetrics;
        ShowMessage($"Mining metrics {(ShowMetrics ? "enabled" : "disabled")}.");
    }
    private Block? SelectBlock(bool allowGenesis = true)
    {
        int minIndex = allowGenesis ? 0 : 1;

        Console.WriteLine();
        PrintInfo("Available", $"{minIndex} - {_blockchainService.Chain.Count - 1}");

        WriteColored("\n  Block ID: ", ConsoleColor.Cyan);

        if (!int.TryParse(Console.ReadLine(), out int id) || id < minIndex)
            return null;

        return _blockchainService.Chain.Find(block => block.Index == id);
    }
    private void ShowMessage(string message, bool success = true)
    {
        _statusMessage = message;
        _statusSuccess = success;
    }
    private void ShowStatusBar()
    {
        if (string.IsNullOrWhiteSpace(_statusMessage))
            return;

        Console.WriteLine();

        WriteColored(
            _statusSuccess ? "  [+] " : "  [!] ",
            _statusSuccess ? ConsoleColor.Green : ConsoleColor.Red);

        Console.WriteLine(_statusMessage);
    }
    private void WaitForKey()
    {
        WriteColored("\n  Press any key to continue...", ConsoleColor.DarkGray);
        Console.ReadKey(true);
    }
    private void PrintHeader(string title)
    {
        WriteColored($"\n  {title}\n", ConsoleColor.Cyan);
        PrintSeparator();
    }
    private void PrintSeparator()
    {
        WriteColored("----------------------------------------------\n", ConsoleColor.DarkGray);
    }
    private void PrintOption(int number, string title)
    {
        WriteColored($"  [{number}] ", ConsoleColor.Cyan);
        Console.WriteLine(title);
    }
    private void PrintInfo(string label, string value)
    {
        WriteColored($"  {label,-14}: ", ConsoleColor.DarkGray);
        Console.WriteLine(value);
    }
    private void PrintStatus(string message, ConsoleColor color)
    {
        WriteColored($"\n  {message}\n", color);
    }
    private void WriteColored(string text, ConsoleColor color)
    {
        var previous = Console.ForegroundColor;
        Console.ForegroundColor = color;
        Console.Write(text);
        Console.ForegroundColor = previous;
    }
}