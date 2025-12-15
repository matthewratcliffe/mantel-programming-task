namespace Domain;

public sealed class VirusScanResult(bool isClean, List<string>? engine = null, string? message = null)
{
    public bool IsClean { get; } = isClean;
    public List<string>? EnginesUsed { get; } = engine;
    public string? Message { get; } = message;
}