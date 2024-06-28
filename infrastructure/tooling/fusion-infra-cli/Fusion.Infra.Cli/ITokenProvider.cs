// See https://aka.ms/new-console-template for more information
using Azure.Core;
using System.Net.Http.Json;
/// <summary>
/// Get token using microsoft authentication. For now this is using default credentials.
/// </summary>
public interface ITokenProvider
{
    Task<AccessToken> GetAccessToken(string resource);
}
