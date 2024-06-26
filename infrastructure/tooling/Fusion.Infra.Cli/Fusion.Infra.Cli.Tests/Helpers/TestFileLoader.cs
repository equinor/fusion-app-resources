using Newtonsoft.Json;

namespace Fusion.Infra.Cli.Tests
{
    public class TestFileLoader : IFileLoader
    {
        public string content;

        public TestFileLoader(string content)
        {
            this.content = content; 
        }

        public static TestFileLoader FromJson(object json)
        {
            return new TestFileLoader(JsonConvert.SerializeObject(json, Formatting.Indented));
        }

        public bool Exists(string path)
        {
            return true;
        }

        public string GetContent(string path)
        {
            return content;
        }
    }


    
}