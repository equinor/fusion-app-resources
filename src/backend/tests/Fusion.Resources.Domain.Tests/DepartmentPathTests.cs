using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Fusion.Resources.Domain.Tests
{
    public class DepartmentPathTests
    {

        [Fact]
        public void ReturnAllParents()
        {
            var fullPath = "PDP PRD PMC PCA PCA1";

            var path = new DepartmentPath(fullPath);

            var allParents = path.GetAllParents();

            allParents
                .Should()
                .ContainInOrder("PDP PRD PMC PCA", "PDP PRD PMC", "PDP PRD", "PDP");
        }
    }
}
