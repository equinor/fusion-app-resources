using Fusion.ApiClients.Org;
using Fusion.AspNetCore.OData;
using Fusion.Integration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Fusion.Testing.Mocks.ContextService
{
    public class ContextResolverMock : IFusionContextResolver
    {
        public List<FusionContext> AvailableContexts { get; } = new List<FusionContext>();
        public List<FusionContext> AvailableRelations { get; } = new List<FusionContext>();


        public Task<FusionContext> GetContextAsync(Guid contextId)
        {
            var context = AvailableContexts.FirstOrDefault(c => c.Id == contextId);

            if (context is null)
                throw new ContextNotFoundError(contextId);

            return Task.FromResult(context);
        }

        public Task<IEnumerable<FusionContext>> GetContextRelationsAsync(Guid contextId, FusionContextType contextType = null)
        {
            var relations = AvailableRelations.Where(x => x.ExternalId == contextId.ToString() || x.Id == contextId);

            if (contextType != null)
                relations = relations.Where(x => x.Type == contextType);

            return Task.FromResult(relations);
        }

        public Task<IEnumerable<FusionRelatedContext>> QueryContextRelationssAsync(FusionContext context, Action<ContextApiQuery> query = null)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<FusionContext>> QueryContextsAsync(Action<ContextApiQuery> query)
        {
            var apiQuery = new ContextApiQuery();
            query(apiQuery);

            var queryString = apiQuery.QueryString;
            var match = Regex.Match(queryString ?? "", @"\$filter=([^&]*)");

            var filter = Fusion.AspNetCore.OData.ODataParser.Parse(match.Groups[1].Value);
            var odataQuery = new ODataQueryParams() { Filter = filter };

            var contextQuery = AvailableContexts.AsQueryable();

            contextQuery = contextQuery.ApplyODataFilters(odataQuery, m =>
            {
                m.MapField("id", c => c.Id);
                m.MapField("title", c => c.Title);
                m.MapField("externalid", c => c.ExternalId);
                m.MapField("type", c => c.Type);

                if (filter.GetFilters().Any(f => f.Field.StartsWith("value.")))
                    throw new NotImplementedException();

                // Map all value.* to json support
                //message.Query.Filter.GetFilters().Where(f => f.Field.StartsWith("value."))
                //    .ToList()
                //    .ForEach(f =>
                //    {
                //        m.MapJsonField(f, nameof(ContextBase.Value), "value.");
                //    });
            });

            var contexts = contextQuery.ToList();
            return Task.FromResult(contexts.AsEnumerable());
        }

        public Task<FusionContext> ResolveContextAsync(Guid contextId)
        {
            var context = AvailableContexts.FirstOrDefault(c => c.Id == contextId);
            return Task.FromResult(context);
        }

        public Task<FusionContext> ResolveContextAsync(ContextIdentifier identifier, FusionContextType type = null)
        {
            var ctx = default(FusionContext);

            switch (identifier.Type)
            {
                case ContextIdentifier.IdentifierType.ContextId:
                    ctx = AvailableContexts.FirstOrDefault(c => c.Id == identifier.UniqueId);
                    break;

                case ContextIdentifier.IdentifierType.ExternalId:
                    ctx = AvailableContexts.Where(c => type == null || string.Equals(c.Type.Name, type.Name, StringComparison.OrdinalIgnoreCase))
                        .FirstOrDefault(c => string.Equals(c.ExternalId, identifier.Identifier, StringComparison.OrdinalIgnoreCase));
                    break;

                case ContextIdentifier.IdentifierType.Ambiguous:
                    ctx = AvailableContexts.FirstOrDefault(c => c.Id == identifier.UniqueId);

                    if (ctx is null)
                    {
                        ctx = AvailableContexts.Where(c => type == null || string.Equals(c.Type.Name, type.Name, StringComparison.OrdinalIgnoreCase))
                            .FirstOrDefault(c => string.Equals(c.ExternalId, identifier.Identifier, StringComparison.OrdinalIgnoreCase));
                    }
                    break;
            }

            return Task.FromResult(ctx);

        }

        public ContextResolverMock AddContext(ApiProjectV2 orgChart)
        {
            AvailableContexts.Add(new FusionContext()
            {
                ExternalId = $"{orgChart.ProjectId}",
                Id = Guid.NewGuid(),
                Title = orgChart.Name,
                Source = "OrgChart",
                Type = FusionContextType.OrgChart,
                Value = new { }
            });
            return this;
        }
        public ContextResolverMock AddRelation(Guid externalId, FusionContextType type)
        {
            AvailableRelations.Add(new FusionContext()
            {
                ExternalId = $"{externalId}",
                Id = Guid.NewGuid(),
                Title = nameof(type),
                Source = nameof(type),
                Type = type,
                Value = new { }
            });
            return this;
        }

    }
}
