using Fusion.Testing;
using Xunit.Abstractions;

namespace Fusion.Summary.Api.Tests.IntegrationTests.Base;

public class TestBase : IAsyncLifetime
{
    private TestLoggingScope? _output;

    /// <summary>
    ///     Set this to log output api calls and responses from tests.
    /// </summary>
    public void SetOutput(ITestOutputHelper output)
    {
        _output = new TestLoggingScope(output);
    }

    public virtual Task InitializeAsync()
    {
        return Task.CompletedTask;
    }

    public virtual Task DisposeAsync()
    {
        return Task.CompletedTask;
    }
}