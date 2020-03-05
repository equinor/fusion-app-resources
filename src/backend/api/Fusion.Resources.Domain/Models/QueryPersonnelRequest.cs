using Fusion.Resources.Database.Entities;
using System;

namespace Fusion.Resources.Domain
{
    public class QueryPersonnelRequest
    {
        public QueryPersonnelRequest(DbContractorRequest request, QueryPositionRequest position)
        {
            Id = request.Id;
            Position = position;
            Person = new QueryContractPersonnel(request.Person);

            Description = request.Description;
            State = request.State;

            Project = new QueryProject(request.Project);
            Contract = new QueryContract(request.Contract);

            Created = request.Created;
            Updated = request.Updated;
            CreatedBy = new QueryPerson(request.CreatedBy);
            UpdatedBy = QueryPerson.FromEntityOrDefault(request.UpdatedBy);
        }
        public Guid Id { get; set; }

        public DbRequestState State { get; set; }

        public string Description { get; set; }


        public QueryContractPersonnel Person { get; set; }
        public QueryPositionRequest Position { get; set; }

        public QueryPerson CreatedBy { get; set; }
        public QueryPerson UpdatedBy { get; set; }
        public DateTimeOffset Created { get; set; }
        public DateTimeOffset? Updated { get; set; }

        public QueryProject Project { get; set; }
        public QueryContract Contract { get; set; }
    }
}

