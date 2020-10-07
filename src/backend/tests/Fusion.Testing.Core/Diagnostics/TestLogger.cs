namespace Fusion.Testing
{
    public class TestLogger
    {
        public static void TryLog(string message) => TestLoggingScope.Current?.WriteLine(message);
    }
}
