using Newtonsoft.Json;

namespace Fusion.Testing
{
    public class TestLogger
    {
        public static void TryLog(string message) => TestLoggingScope.Current?.WriteLine(message);
        public static void TryLogObject(object message) => TestLoggingScope.Current?.WriteLine(JsonConvert.SerializeObject(message, Formatting.Indented));
    }
}
