﻿using System;
using System.Collections.Generic;
using System.Linq;
using AdaptiveCards;

namespace Fusion.Summary.Functions.CardBuilder;

public class AdaptiveCardBuilder
{
    private readonly AdaptiveCard _adaptiveCard = new(new AdaptiveSchemaVersion(1, 2));

    public AdaptiveCardBuilder AddHeading(string text)
    {
        var heading = new AdaptiveTextBlock
        {
            Text = text,
            Wrap = true,
            HorizontalAlignment = AdaptiveHorizontalAlignment.Center,
            Separator = true,
            Size = AdaptiveTextSize.Large,
            Weight = AdaptiveTextWeight.Bolder
        };
        _adaptiveCard.Body.Add(heading);
        return this;
    }

    public AdaptiveCardBuilder AddTextRow(string valueText, string headerText, string customText = "", GoToAction? goToAction = null)
    {
        var container = new AdaptiveContainer()
        {
            Separator = true,
            Items = new List<AdaptiveElement>()
            {
                new AdaptiveTextBlock
                {
                    Text = $"{valueText} {customText}",
                    Wrap = true,
                    HorizontalAlignment = AdaptiveHorizontalAlignment.Center,
                    Size = AdaptiveTextSize.ExtraLarge
                },
                new AdaptiveTextBlock
                {
                    Text = headerText,
                    Wrap = true,
                    HorizontalAlignment = AdaptiveHorizontalAlignment.Center
                }
            }
        };

        if (goToAction != null)
        {
            var actionSet = new AdaptiveActionSet();
            var action = new AdaptiveOpenUrlAction()
            {
                Title = goToAction.Title,
                Url = new Uri(goToAction.Url)
            };

            actionSet.Actions.Add(action);
            container.Items.Add(actionSet);
        }

        _adaptiveCard.Body.Add(container);
        return this;
    }


    public AdaptiveCardBuilder AddGrid(string headerText, string subtitleText, IEnumerable<GridColumn> columnsEnumerable, GoToAction? goToAction = null, int? maxItems = 10)
    {
        var columns = columnsEnumerable.ToList();
        var listContainer = new AdaptiveContainer
        {
            Separator = true
        };

        var header = new AdaptiveTextBlock
        {
            Weight = AdaptiveTextWeight.Bolder,
            Text = headerText,
            Wrap = true,
            Size = AdaptiveTextSize.Large,
            HorizontalAlignment = AdaptiveHorizontalAlignment.Center
        };

        var subtitle = new AdaptiveTextBlock
        {
            Text = subtitleText,
            Wrap = true,
            HorizontalAlignment = AdaptiveHorizontalAlignment.Center
        };


        var grid = new AdaptiveColumnSet();
        var maxItemsReached = false;
        var totalRows = columns.FirstOrDefault()?.Cells.Count(c => !c.IsHeader) ?? 0;

        foreach (var column in columns)
        {
            maxItemsReached = false;
            var cellCount = 0;
            var rows = new List<AdaptiveElement>();

            foreach (var gridCell in column.Cells)
            {
                if (maxItems.HasValue && cellCount >= maxItems)
                {
                    maxItemsReached = true;
                    break;
                }

                var cell = new AdaptiveTextBlock
                {
                    Text = gridCell.Value,
                    Wrap = true,
                    HorizontalAlignment = gridCell.Alignment,
                    IsSubtle = gridCell.IsHeader,
                    Id = gridCell.IsHeader ? "isHeader" : "isCell"
                };

                rows.Add(cell);
                if (!gridCell.IsHeader)
                    cellCount++;
            }

            var gridColumn = new AdaptiveColumn
            {
                Width = column.Width,
                Items = rows
            };

            grid.Columns.Add(gridColumn);
        }

        // Add empty row so that things are aligned correctly
        if (columns.SelectMany(c => c.Cells).All(c => c.IsHeader))
        {
            var rows = new List<AdaptiveElement>
            {
                new AdaptiveTextBlock
                {
                    Text = "-",
                    Wrap = true,
                    HorizontalAlignment = AdaptiveHorizontalAlignment.Left,
                    Id = "isCell"
                },
                new AdaptiveTextBlock
                {
                    Text = "-",
                    Wrap = true,
                    HorizontalAlignment = AdaptiveHorizontalAlignment.Right,
                    Id = "isCell"
                }
            };

            grid.Columns.Add(new AdaptiveColumn
            {
                Width = AdaptiveColumnWidth.Auto,
                Items = rows
            });
        }

        listContainer.Items.Add(header);
        listContainer.Items.Add(subtitle);

        if (goToAction != null)
        {
            var actionSet = new AdaptiveActionSet();
            var action = new AdaptiveOpenUrlAction()
            {
                Title = goToAction.Title,
                Url = new Uri(goToAction.Url)
            };
            actionSet.Actions.Add(action);
            listContainer.Items.Add(actionSet);
        }

        listContainer.Items.Add(grid);

        // If no data is present, add a "None" text
        if (columns.SelectMany(c => c.Cells).All(c => c.IsHeader || string.IsNullOrEmpty(c.Value)))
        {
            listContainer.Items.Add(new AdaptiveTextBlock
            {
                Text = "None",
                Wrap = true,
                HorizontalAlignment = AdaptiveHorizontalAlignment.Center
            });
        }

        if (maxItemsReached && maxItems.HasValue)
        {
            listContainer.Items.Add(new AdaptiveTextBlock
            {
                Text = $"And {totalRows - maxItems} more...",
                Wrap = true,
                HorizontalAlignment = AdaptiveHorizontalAlignment.Center
            });
        }


        _adaptiveCard.Body.Add(listContainer);
        return this;
    }


    public AdaptiveCardBuilder AddListContainer(string headerText,
        List<List<ListObject>> objectLists)
    {
        var listContainer = new AdaptiveContainer
        {
            Separator = true,
        };

        var header = new AdaptiveTextBlock
        {
            Weight = AdaptiveTextWeight.Bolder,
            Text = headerText,
            Wrap = true,
            Size = AdaptiveTextSize.Large,
            HorizontalAlignment = AdaptiveHorizontalAlignment.Center
        };

        var rows = new List<AdaptiveColumnSet>();

        foreach (var listObject in objectLists)
        {
            var row = new AdaptiveColumnSet()
            {
                Columns = listObject.Select(o => new AdaptiveColumn()
                {
                    Width = AdaptiveColumnWidth.Stretch,
                    Items = new List<AdaptiveElement>
                    {
                        new AdaptiveTextBlock
                        {
                            Text = o.Value,
                            Wrap = true,
                            HorizontalAlignment = o.Alignment
                        }
                    }
                }).ToList()
            };

            rows.Add(row);
        }

        listContainer.Items.Add(header);
        listContainer.Items.AddRange(rows);


        _adaptiveCard.Body.Add(listContainer);
        return this;
    }

    public AdaptiveCardBuilder AddActionButton(string title, string url)
    {
        var action = new AdaptiveOpenUrlAction()
        {
            Title = title,
            Url = new Uri(url)
        };

        _adaptiveCard.Actions.Add(action);

        return this;
    }

    public AdaptiveCardBuilder AddNewLine()
    {
        var container = new AdaptiveContainer()
        {
            Separator = true
        };

        _adaptiveCard.Body.Add(container);

        return this;
    }

    public AdaptiveCard Build()
    {
        return _adaptiveCard;
    }


    public class ListObject
    {
        public string Value { get; set; }
        public AdaptiveHorizontalAlignment Alignment { get; set; }
    }
}

public class GridColumn
{
    public ICollection<GridCell> Cells { get; set; }
    public string Width { get; set; } = AdaptiveColumnWidth.Auto;
}

public class GridCell
{
    public GridCell(bool isHeader, string value)
    {
        IsHeader = isHeader;
        Value = value;
    }

    public bool IsHeader { get; set; }
    public string Value { get; set; }
    public AdaptiveHorizontalAlignment Alignment { get; set; }
}

public class GoToAction
{
    public string Title { get; set; }
    public string Url { get; set; }
}