using System.Text.Json.Serialization;
using Fusion.Summary.Api.Domain.Queries.Base;

namespace Fusion.Summary.Api.Controllers.ApiModels;

public class ApiCollection<TApiModel>
{
    public ApiCollection()
    {
        Items = Array.Empty<TApiModel>();
    }

    public static ApiCollection<TApiModel> FromQueryCollection<TQueryModel>(
        QueryCollection<TQueryModel> queryCollection, Func<TQueryModel, TApiModel> transformationFunc)
    {
        return new ApiCollection<TApiModel>()
        {
            Items = queryCollection.Select(transformationFunc).ToArray(),
            Top = queryCollection.Top,
            Skip = queryCollection.Skip,
            TotalCount = queryCollection.TotalCount
        };
    }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Top { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Skip { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? TotalCount { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? HasMoreItems => (TotalCount is not null && Skip is not null && Top is not null)
        ? TotalCount > Skip + Top
        : null;

    public ICollection<TApiModel> Items { get; set; }
}