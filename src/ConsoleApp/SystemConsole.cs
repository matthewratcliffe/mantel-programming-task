using Application.Interfaces;

namespace ConsoleApp;

public class SystemConsole : IConsole
{
    public void WriteLine(string value = "") => Console.WriteLine(value);
    public ConsoleKeyInfo ReadKey(bool intercept = true) => Console.ReadKey(intercept);
    public void Clear() => Console.Clear();
    public ConsoleColor ForegroundColor { set => Console.ForegroundColor = value; }
    public ConsoleColor BackgroundColor { set => Console.BackgroundColor = value; }
    public void ResetColor() => Console.ResetColor();
}