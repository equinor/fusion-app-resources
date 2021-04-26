using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fusion.Resources.Api
{

    /// <summary>
    /// Wrapper object for working with department paths. 
    /// Allows dynamically traversing/analyzing the path.
    /// </summary>
    public class DepartmentPath
    {
        private readonly string[] path;
        private readonly string fullDepartmentPath;

        public DepartmentPath(string fullDepartmentPath)
        {
            this.fullDepartmentPath = fullDepartmentPath;
            path = fullDepartmentPath.Split(" ");
        }

        public int Level => path.Length;

        public string Parent(int levelsToJump = 1) => string.Join(" ", path.SkipLast(levelsToJump));
        public string GoToLevel(int level) => string.Join(" ", path.Take(level <= 0 ? 1 : level));
    }
}
