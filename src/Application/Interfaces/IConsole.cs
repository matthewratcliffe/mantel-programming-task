namespace Application.Interfaces;

public interface IConsole
{
    void WriteLine(string value = "");
    ConsoleKeyInfo ReadKey(bool intercept = true);
    void Clear();
    ConsoleColor ForegroundColor { set; }
    ConsoleColor BackgroundColor { set; }
    void ResetColor();
}