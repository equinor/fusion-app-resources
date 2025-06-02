using System;
using System.Threading.Tasks;
using Fusion.Services.Org.ApiModels;

namespace Fusion.Resources.Domain
{
    public interface ICompanyResolver
    {
        Task<ApiCompany?> FindCompanyAsync(Guid companyId);
    }
}
