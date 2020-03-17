using System;
using Fusion.Resources.Database.Entities;

#nullable enable

namespace Fusion.Resources.Domain
{
    /// <summary>
    /// Reference to a personnel request where the specific personnel id is used.
    /// 
    /// This entity only holds info related to the request position, not the person.
    /// </summary>
    public class QueryPersonnelRequestReference
    {
        public QueryPersonnelRequestReference(DbContractorRequest request, QueryPositionRequest position)
        {
            Id = request.Id;
            PersonnelId = request.Person.PersonId;
            Position = position;

            State = request.State;

            Project = new QueryProject(request.Project);
            Contract = new QueryContract(request.Contract);

            Created = request.Created;
            Updated = request.Updated;
        }

        public Guid Id { get; set; }
        public Guid PersonnelId { get; set; }

        public DbRequestState State { get; set; }

        public QueryPositionRequest Position { get; set; }

        public DateTimeOffset Created { get; set; }
        public DateTimeOffset? Updated { get; set; }

        public QueryProject Project { get; set; }
        public QueryContract Contract { get; set; }
    }
}
