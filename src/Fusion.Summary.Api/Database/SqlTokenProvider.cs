using Fusion.Infrastructure.Database;
using Fusion.Integration.Configuration;

namespace Fusion.Summary.Api.Database;

public class SqlTokenProvider : ISqlTokenProvider
{
    private readonly IFusionTokenProvider fusionTokenProvider;

    public SqlTokenProvider(IFusionTokenProvider fusionTokenProvider)
    {
        this.fusionTokenProvider = fusionTokenProvider;
    }

    public Task<string> GetAccessTokenAsync()
    {
        return fusionTokenProvider.GetApplicationTokenAsync("https://database.windows.net/");
    }
}