using System;
using System.Linq;
using System.Net.Http;
using Fusion.Testing;

namespace Fusion.Resources.Api.Tests;

public static class TestClientHttpResponseExtensions
{
    public static void CheckAllowHeader(this TestClientHttpResponse<dynamic> result, string allowed)
    {
        var expectedVerbs = allowed
            .Split(",", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(x =>
            {
                if (x.StartsWith('!'))
                    return new { Key = "disallowed", Method = new HttpMethod(x.Substring(1)) };
                else
                    return new { Key = "allowed", Method = new HttpMethod(x) };
            })
            .ToLookup(x => x.Key, x => x.Method);

        if (expectedVerbs["allowed"].Any())
            result.Should().HaveAllowHeaders(expectedVerbs["allowed"].ToArray());

        if (expectedVerbs["disallowed"].Any())
            result.Should().NotHaveAllowHeaders(expectedVerbs["disallowed"].ToArray());
    }
}