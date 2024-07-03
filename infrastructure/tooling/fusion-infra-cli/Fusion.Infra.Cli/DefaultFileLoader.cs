// See https://aka.ms/new-console-template for more information
using Microsoft.Extensions.Logging;

public class DefaultFileLoader : IFileLoader
{
    private readonly ILogger<IFileLoader> logger;

    public DefaultFileLoader(ILogger<IFileLoader> logger)
    {
        this.logger = logger;
    }

    public bool Exists(string path)
    {
        var exists = File.Exists(path);
        if (!exists)
        {
            logger.LogWarning("File does not exist: {FilePath}", path);
        }
        return exists;
    }

    public string GetContent(string path)
    {
        try
        {
            return File.ReadAllText(path);
        } catch (Exception ex)
        {
            logger.LogError("Could not read content from file {FilePath}: {ErrorMessage}", path, ex.Message);
            throw;
        }
    }
}