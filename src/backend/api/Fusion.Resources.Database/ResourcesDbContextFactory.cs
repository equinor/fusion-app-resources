using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using System;

namespace Fusion.Resources.Database
{
    public class ResourcesDbContextFactory : IDesignTimeDbContextFactory<ResourcesDbContext>
    {
        public ResourcesDbContext CreateDbContext(string[] args)
        {
            Console.WriteLine("Using designtime factory...");

            var optionsBuilder = new DbContextOptionsBuilder<ResourcesDbContext>();
            optionsBuilder.UseSqlServer("Data Source=blog.db");

            return new ResourcesDbContext(optionsBuilder.Options);
        }
    }
}
