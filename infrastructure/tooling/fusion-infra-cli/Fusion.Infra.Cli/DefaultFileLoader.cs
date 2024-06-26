// See https://aka.ms/new-console-template for more information
public class DefaultFileLoader : IFileLoader
{
    public bool Exists(string path)
    {
        return File.Exists(path);
    }

    public string GetContent(string path)
    {
        return File.ReadAllText(path);
    }
}