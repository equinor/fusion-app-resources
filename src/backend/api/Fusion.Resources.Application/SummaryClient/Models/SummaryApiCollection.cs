namespace Fusion.Resources.Application.SummaryClient.Models;

public class SummaryApiCollectionDto<T>
{
    public int? Top { get; set; }

    public int? Skip { get; set; }

    public int? TotalCount { get; set; }

    public bool? HasMoreItems { get; set; }

    public T[] Items { get; set; } = [];
}