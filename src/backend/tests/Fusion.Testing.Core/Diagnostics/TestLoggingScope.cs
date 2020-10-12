using System;
using System.Threading;
using Xunit.Abstractions;

namespace Fusion.Testing
{
    public class TestLoggingScope : IDisposable
    {
        private static AsyncLocal<ITestOutputHelper> Logger = new AsyncLocal<ITestOutputHelper>();

        public static ITestOutputHelper Current => Logger.Value;

        public TestLoggingScope(ITestOutputHelper logger)
        {
            Logger.Value = logger;
        }

        public void Dispose()
        {
            Logger.Value = null;
        }
    }
}
