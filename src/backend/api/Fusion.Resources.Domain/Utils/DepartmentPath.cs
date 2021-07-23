using System;
using System.Linq;

namespace Fusion.Resources.Domain
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
            var trimedPath = (fullDepartmentPath ?? "").Trim();

            this.fullDepartmentPath = trimedPath;
            path = trimedPath.Split(" ");
        }

        public int Level => path.Length;

        public string Parent(int levelsToJump = 1) => string.Join(" ", path.SkipLast(levelsToJump));
        public string GoToLevel(int level) => string.Join(" ", path.Take(level <= 0 ? 1 : level));

        public DepartmentPath Intersect(DepartmentPath other)
        {
            var intersection = path.Zip(other.path)
                .TakeWhile(t => t.First.Equals(t.Second, StringComparison.OrdinalIgnoreCase))
                .Select(t => t.First);

            return new DepartmentPath(string.Join(" ", intersection));
        }

        public static bool IsPrefix(DepartmentPath source, DepartmentPath prefix)
            => source.Intersect(prefix).Level > 0;
    }
}
