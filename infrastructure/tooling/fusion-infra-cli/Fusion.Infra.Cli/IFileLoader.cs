// See https://aka.ms/new-console-template for more information
public interface IFileLoader
{
    bool Exists(string path);
    string GetContent(string path);
}
