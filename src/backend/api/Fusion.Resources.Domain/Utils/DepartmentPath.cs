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

        public string GetShortName() => string.Join(' ', path.TakeLast(3));
    }
}
