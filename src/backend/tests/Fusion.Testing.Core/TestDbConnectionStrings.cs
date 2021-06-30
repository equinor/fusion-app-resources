namespace Fusion.Testing
{
    public class TestDbConnectionStrings
    {
        public static string LocalDb(string db, string mdfPath) => $"Server=(localdb)\\mssqllocaldb;Database={db};Trusted_Connection=True;AttachDBFileName={mdfPath}";
        public static string LocalDb(string db) => $"Server=(localdb)\\mssqllocaldb;Database={db};Trusted_Connection=True";

    }
}
