using System;
using System.Threading.Tasks;

namespace Fusion.Resources.Domain
{
    public interface ICompanyResolver
    {
        Task<Fusion.ApiClients.Org.ApiCompanyV2> FindCompanyAsync(Guid companyId);
    }
}
