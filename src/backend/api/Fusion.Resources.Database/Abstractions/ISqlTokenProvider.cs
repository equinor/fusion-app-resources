using System.Threading.Tasks;

namespace Fusion.Resources.Database
{
    public interface ISqlTokenProvider
    {
        Task<string> GetAccessTokenAsync();
    }
}
