using System;
using System.Collections.Generic;
using System.Linq;
using AdaptiveCards;

namespace Fusion.Summary.Functions.CardBuilder;

public class AdaptiveCardBuilder
{
    private readonly AdaptiveCard _adaptiveCard = new(new AdaptiveSchemaVersion(1, 0));

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

    public AdaptiveCardBuilder AddColumnSet(params AdaptiveCardColumn[] columns)
    {
        var columnSet = new AdaptiveColumnSet
        {
            Columns = columns.Select(col => col.Column).ToList(),
            Separator = true
        };
        _adaptiveCard.Body.Add(columnSet);
        return this;
    }

    public AdaptiveCardBuilder AddListContainer(string headerText,
        List<List<ListObject>> objectLists)
    {
        var listContainer = new AdaptiveContainer
        {
            Separator = true,
            Items = new List<AdaptiveElement>
            {
                new AdaptiveTextBlock
                {
                    Weight = AdaptiveTextWeight.Bolder,
                    Text = headerText,
                    Wrap = true,
                    Size = AdaptiveTextSize.Large
                },
                new AdaptiveColumnSet
                {
                    Columns = new List<AdaptiveColumn>
                    {
                        new()
                        {
                            Width = AdaptiveColumnWidth.Stretch,
                            Items = new List<AdaptiveElement>
                            {
                                new AdaptiveCardList(objectLists).List
                            }
                        }
                    }
                }
            }
        };
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

    public class AdaptiveCardColumn
    {
        public AdaptiveColumn Column { get; }

        public AdaptiveCardColumn(string numberText, string headerText, string customText = "")
        {
            Column = new AdaptiveColumn
            {
                Width = AdaptiveColumnWidth.Stretch,
                Separator = true,
                Spacing = AdaptiveSpacing.Medium,
                Items = new List<AdaptiveElement>
                {
                    new AdaptiveTextBlock
                    {
                        Text = $"{numberText} {customText}",
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
        }
    }

    private class AdaptiveCardList
    {
        public AdaptiveContainer List { get; }

        public AdaptiveCardList(List<List<ListObject>> objectLists)
        {
            var listItems = new List<AdaptiveElement>();
            foreach (var objects in objectLists)
            {
                var columns = new List<AdaptiveColumn>();
                foreach (var o in objects)
                {
                    var column = new AdaptiveColumn()
                    {
                        Width = AdaptiveColumnWidth.Stretch,
                        Items = new List<AdaptiveElement>
                        {
                            new AdaptiveTextBlock
                            {
                                Text = $"{o.Value} ", Wrap = true,
                                HorizontalAlignment = o.Alignment
                            }
                        }
                    };
                    columns.Add(column);
                }

                listItems.Add(new AdaptiveColumnSet()
                {
                    Columns = columns
                });
            }

            List = new AdaptiveContainer
            {
                Items = listItems
            };
        }
    }

    public class ListObject
    {
        public string Value { get; set; }
        public AdaptiveHorizontalAlignment Alignment { get; set; }
    }
}