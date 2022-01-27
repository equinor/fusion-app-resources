using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Fusion.Resources.Database.Entities
{
    public class DbContractPersonnelReplacement
    {
        public Guid Id { get; set; }

        public Guid ProjectId { get; set; }
        public Guid ContractId { get; set; }
        public string Message { get; set; } = null!;

        [MaxLength(50)]
        public string? ChangeType { get; set; }
        [MaxLength(200)] 
        public string? UPN { get; set; }
        [MaxLength(200)] 
        public string? FromPerson { get; set; }
        [MaxLength(200)] 
        public string? ToPerson { get; set; }

        public DateTimeOffset Created { get; set; }
        [MaxLength(200)] 
        public string? CreatedBy { get; set; }

        public static void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DbContractPersonnelReplacement>(x =>
            {
                x.HasIndex(e => new { e.ProjectId, e.ContractId }).IsClustered(false)
                    .IncludeProperties(e => new
                    {
                        e.UPN,
                        e.FromPerson,
                        e.ToPerson,
                        e.ChangeType,
                        ReplacedTimestamp = e.Created,
                        ReplacedBy = e.CreatedBy
                    });
            });
        }
    }
}