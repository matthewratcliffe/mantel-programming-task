namespace Application.Interfaces;

public interface IFileReader
{
    public Task<byte[]> ReadAllBytes(string path);
}