namespace Fusion.Resources.Functions.Common.Integration.Http
{

    public class UrlUtility
    {

        public static string CombineUrls(string baseUrlPath, params string[] nodes)
        {
            string baseUrl = CombineUrl(baseUrlPath, nodes.First());
            nodes.Skip(1).ToList().ForEach(part => baseUrl = CombineUrl(baseUrl, part));
            return baseUrl;
        }

        public static string CombineUrl(string baseUrlPath, string additionalNodes)
        {
            if (baseUrlPath == null)
            {
                return additionalNodes;
            }
            if (additionalNodes == null)
            {
                return additionalNodes;
            }

            if (baseUrlPath.EndsWith("/", System.StringComparison.OrdinalIgnoreCase))
            {
                if (additionalNodes.StartsWith("/", System.StringComparison.OrdinalIgnoreCase))
                {
                    baseUrlPath = baseUrlPath.TrimEnd(new char[]
                    {
                        '/'
                    });
                }
                return baseUrlPath + additionalNodes;
            }
            if (additionalNodes.StartsWith("/", System.StringComparison.OrdinalIgnoreCase))
            {
                return baseUrlPath + additionalNodes;
            }
            return baseUrlPath + "/" + additionalNodes;
        }

        public static string GetServerUrl(string url)
        {
            Uri serverUrl = new Uri(url);

            return serverUrl.GetLeftPart(UriPartial.Authority);
        }
        public static string GetAbsoluteDocumentUrl(string site, string serverRelativeUrl)
        {
            string left = GetServerUrl(site);
            return UrlUtility.CombineUrl(left, serverRelativeUrl);
        }
    }

}
