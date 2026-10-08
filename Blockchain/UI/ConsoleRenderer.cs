using Blockchain.Menus;

namespace Blockchain.UI;

public class ConsoleRenderer
{
    private readonly MenuContext _context;

    public ConsoleRenderer(MenuContext context)
    {
        _context = context;
    }

    public void PrintHeader(string title)
    {
        WriteColored($"\n  {title}\n", ConsoleColor.Cyan);
        PrintSeparator();
    }
    public void PrintSeparator()
    {
        WriteColored("----------------------------------------------\n", ConsoleColor.DarkGray);
    }
    public void PrintOption(int number, string title)
    {
        WriteColored($"  [{number}] ", ConsoleColor.Cyan);
        Console.WriteLine(title);
    }
    public void PrintInfo(string label, string value)
    {
        WriteColored($"  {label,-14}: ", ConsoleColor.DarkGray);
        Console.WriteLine(value);
    }
    public void PrintStatus(string message, ConsoleColor color)
    {
        WriteColored($"\n  {message}\n", color);
    }
    public void WriteColored(string text, ConsoleColor color)
    {
        var previous = Console.ForegroundColor;
        Console.ForegroundColor = color;
        Console.Write(text);
        Console.ForegroundColor = previous;
    }
    public void ShowStatusBar()
    {
        if (string.IsNullOrWhiteSpace(_context.StatusMessage))
            return;

        Console.WriteLine();

        WriteColored(
            _context.StatusSuccess ? "  [+] " : "  [!] ",
            _context.StatusSuccess ? ConsoleColor.Green : ConsoleColor.Red);

        Console.WriteLine(_context.StatusMessage);
        _context.ClearStatus();
    }
    public void WaitForKey()
    {
        WriteColored("\n  Press any key to continue...", ConsoleColor.DarkGray);
        Console.ReadKey(true);
    }
}