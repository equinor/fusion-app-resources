using Fusion.Summary.Api.Domain.Models;

namespace Fusion.Summary.Api.Controllers.ApiModels;

public class ApiReportMetaData
{
    public Guid Id { get; set; }
    public DateTime Period { get; set; }

    public static ApiReportMetaData FromQueryReportMetaData(QueryReportMetaData queryReportMetaData)
    {
        return new ApiReportMetaData
        {
            Id = queryReportMetaData.Id,
            Period = queryReportMetaData.Period
        };
    }
}