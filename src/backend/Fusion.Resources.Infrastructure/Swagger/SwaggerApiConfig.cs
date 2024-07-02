using System.Collections.Generic;

namespace Microsoft.Extensions.DependencyInjection
{
    public class SwaggerApiConfig
    {
        public string Title { get; set; } = null!;
        public List<int> EnabledVersions { get; } = new List<int>();
        public bool EnablePreview { get; set; }
    }
}
