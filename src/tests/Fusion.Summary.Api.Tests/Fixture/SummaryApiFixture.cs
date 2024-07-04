using Fusion.Summary.Api.Database;
using Fusion.Testing;
using Fusion.Testing.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Fusion.Summary.Api.Tests.Fixture;

public class SummaryApiFixture : IDisposable
{
    public readonly SummaryWebAppFactory ApiFactory;
    public FusionTestFixture Fusion { get; }

    public TestUser ResourcesFullControlUser { get; }

    public TestUser CoreAppUser { get; }

    /// <summary>
    ///     Uses <see cref="ResourcesFullControlUser" /> as the user.
    /// </summary>
    public TestClientScope AdminScope() => new(ResourcesFullControlUser);

    /// <summary>
    ///     Uses <see cref="CoreAppUser" /> as the user.
    /// </summary>
    public TestClientScope CoreAppScope() => new(CoreAppUser);
    public TestClientScope UserScope(TestUser profile) => new(profile);

    public SummaryApiFixture()
    {
        Fusion = new FusionTestFixture();
        ApiFactory = new SummaryWebAppFactory(Fusion);

        ResourcesFullControlUser = Fusion.CreateUser()
            .WithGlobalRole("Fusion.Resources.FullControl");

        CoreAppUser = Fusion.CreateUser()
            .AsApplication(Guid.Parse(TestConstants.APP_CLIENT_ID));
    }


    private HttpClient? _client { get; set; }

    /// <summary>
    ///     Get a clean http client.
    /// </summary>
    public HttpClient GetClient()
    {
        if (_client is not null)
            return _client;

        _client = ApiFactory.CreateClient();

        return _client;
    }

    public void Dispose()
    {
        using var scope = ApiFactory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SummaryDbContext>();
        db.Database.EnsureDeleted();
    }

    internal DbScope DbScope() => new DbScope(ApiFactory.Services);
}

public sealed class DbScope : IDisposable
{
    private readonly IServiceScope scope;
    public SummaryDbContext DbContext { get; }

    public DbScope(IServiceProvider apiServices)
    {
        scope = apiServices.CreateScope();
        DbContext = scope.ServiceProvider.GetRequiredService<SummaryDbContext>();
    }

    public void Dispose()
    {
        scope.Dispose();
    }
}