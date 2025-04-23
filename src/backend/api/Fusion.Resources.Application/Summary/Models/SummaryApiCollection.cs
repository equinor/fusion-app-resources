namespace Fusion.Resources.Application.Summary.Models;

internal class SummaryApiCollection<T>
{
    public int? Top { get; set; }

    public int? Skip { get; set; }

    public int? TotalCount { get; set; }

    public bool? HasMoreItems { get; set; }

    public T[] Items { get; set; } = [];
}