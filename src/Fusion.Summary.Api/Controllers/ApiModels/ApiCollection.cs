using System.Text.Json.Serialization;
using Fusion.Summary.Api.Domain.Queries.Base;

namespace Fusion.Summary.Api.Controllers.ApiModels;

public class ApiCollection<T>
{
    public ApiCollection()
    {
        Items = Array.Empty<T>();
    }

    public ApiCollection(IEnumerable<T> items)
    {
        Items = items.ToArray();
    }

    public ApiCollection(QueryCollection<T> queryCollection)
    {
        Items = queryCollection.ToArray();
        Top = queryCollection.Top;
        Skip = queryCollection.Skip;
        TotalCount = queryCollection.TotalCount;
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

    public ICollection<T> Items { get; set; }
}