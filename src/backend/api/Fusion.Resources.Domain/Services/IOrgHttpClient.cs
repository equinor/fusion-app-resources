using System.Net.Http;
using System.Threading.Tasks;

namespace Fusion.Resources.Domain.Services;

public interface IOrgHttpClient
{
    Task<RequestResponse<T>> SendAsync<T>(HttpRequestMessage request);
}