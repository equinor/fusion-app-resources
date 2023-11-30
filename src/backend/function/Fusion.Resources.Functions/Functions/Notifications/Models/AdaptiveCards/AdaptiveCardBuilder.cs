using AdaptiveCards;
using System;
using System.Collections.Generic;
using System.Linq;
using static Fusion.Resources.Functions.ApiClients.IResourcesApiClient;

namespace Fusion.Resources.Functions.Functions.Notifications.Models.AdaptiveCards
{
    public class AdaptiveCardBuilder
    {
        private AdaptiveCard _adaptiveCard;


        public AdaptiveCardBuilder()
        {
            _adaptiveCard = new AdaptiveCard(new AdaptiveSchemaVersion(1, 0));  // Setting the version of AdaptiveCards
        }
        public AdaptiveCardBuilder AddHeading(string text)
        {
            var heading = new AdaptiveTextBlock
            {
                Text = text,
                Wrap = true,
                HorizontalAlignment = AdaptiveHorizontalAlignment.Center,
                Separator = true,
                Size = AdaptiveTextSize.Large,
                Weight = AdaptiveTextWeight.Bolder,
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

        public AdaptiveCardBuilder AddListContainer(string headerText, IEnumerable<PersonnelContent> items, string text1, string text2)
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
                    Size = AdaptiveTextSize.Large,

                },
                new AdaptiveColumnSet
                {
                    Columns = new List<AdaptiveColumn>
                    {
                        new AdaptiveColumn
                        {
                            Width = AdaptiveColumnWidth.Stretch,
                            Items = new List<AdaptiveElement>
                            {
                                new AdaptiveCardList(items, text1, text2).List
                            }
                        }
                    }
                }
            }
            };
            _adaptiveCard.Body.Add(listContainer);
            return this;
        }

        public AdaptiveCard Build()
        {
            return _adaptiveCard;
        }

        public class AdaptiveCardColumn
        {
            public AdaptiveColumn Column { get; }

            public AdaptiveCardColumn(string numberText, string headerText, string? customText = null)
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
                    Text = $"{numberText} - {customText ?? ""}",
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

        public class AdaptiveCardList
        {
            public AdaptiveContainer List { get; }

            public AdaptiveCardList(IEnumerable<PersonnelContent> items, string titlePropertyName, string valuePropertyName)
            {
                var listItems = new List<AdaptiveElement>();

                foreach (var item in items)
                {
                    var value1 = typeof(PersonnelContent).GetProperty(titlePropertyName)?.GetValue(item)?.ToString();
                    var value2 = typeof(PersonnelContent).GetProperty(valuePropertyName)?.GetValue(item);
                    var value2Text = "";


                    //FIXME: This is not a good way to differ between the two types of lists
                    if (valuePropertyName != "TotalWorkload")
                    {
                        var dateValue = typeof(PersonnelPosition).GetProperty("AppliesTo")?.GetValue(value2);
                        var dateText = dateValue is DateTime dateTime ? dateTime.ToString("dd/MM/yyyy") : string.Empty;
                        value2Text = $"End date: {dateText}";
                    }
                    else
                    {
                        value2Text = value2.ToString() + "%";
                    }


                    if (!string.IsNullOrEmpty(value1) && value2 != null)
                    {

                        var columnSet = new AdaptiveColumnSet
                        {
                            Columns = new List<AdaptiveColumn>
                        {
                        new AdaptiveColumn
                        {
                            Width = AdaptiveColumnWidth.Stretch,
                            Items = new List<AdaptiveElement>
                            {
                                new AdaptiveTextBlock
                                { Text = $"{value1} ", Wrap = true, HorizontalAlignment = AdaptiveHorizontalAlignment.Left },
                            }
                        },
                        new AdaptiveColumn
                        {
                            Width = AdaptiveColumnWidth.Stretch,
                            Items = new List<AdaptiveElement>
                            {
                                new AdaptiveTextBlock
                                { Text = value2Text, Wrap = true,  HorizontalAlignment = AdaptiveHorizontalAlignment.Right },
                            }
                        }
                    }
                        };
                        listItems.Add(columnSet);

                    }

                }

                List = new AdaptiveContainer
                {
                    Items = listItems
                };
            }
        }
    }
}
