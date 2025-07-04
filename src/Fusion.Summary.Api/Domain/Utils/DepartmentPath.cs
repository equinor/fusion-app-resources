using System;
using System.Linq;
using Microsoft.EntityFrameworkCore.Infrastructure.Internal;
using static System.Math;

namespace Fusion.Summary.Domain
{

    /// <summary>
    /// Wrapper object for working with department paths.
    /// Allows dynamically traversing/analyzing the path.
    /// </summary>
    public class DepartmentPath
    {
        private readonly string[] path;
        private readonly string fullDepartmentPath;

        public DepartmentPath(string? fullDepartmentPath)
        {
            var trimmedPath = (fullDepartmentPath ?? "").Trim();

            this.fullDepartmentPath = trimmedPath;
            path = trimmedPath.Split(" ");
        }

        public int Level => path.Length;

        public DepartmentPath ParentDepartment => new(Parent());

        public string Parent(int levelsToJump = 1) => string.Join(" ", path.SkipLast(levelsToJump));
        private string Current => string.Join(" ", path);
        public string GoToLevel(int level) => string.Join(" ", path.Take(level <= 0 ? 1 : level));

        public bool IsRelevant(string? other)
        {
            if (string.IsNullOrEmpty(other)) return false;

            return IsRelevant(new DepartmentPath(other));
        }

        public bool IsRelevant(DepartmentPath other)
        {
            var levelDelta = Abs(Level - other.Level);

            return (levelDelta <= 2 && fullDepartmentPath.StartsWith(other.fullDepartmentPath, StringComparison.OrdinalIgnoreCase))
                || other.fullDepartmentPath.StartsWith(fullDepartmentPath, StringComparison.OrdinalIgnoreCase);
        }

        public bool IsParent(string? path)
        {
            if (path is null)
                return false;

            return path.StartsWith(fullDepartmentPath, StringComparison.OrdinalIgnoreCase);
        }

        public bool IsDepartment(string? path)
        {
            if (path is null)
                return false;

            return path.Equals(fullDepartmentPath, StringComparison.OrdinalIgnoreCase);
        }

        public string GetShortName() => string.Join(' ', path.TakeLast(3));

        public override string ToString()
        {
            return Current;
        }

        public static implicit operator string(DepartmentPath departmentPath)
        {
            return departmentPath.ToString();
        }
    }
}
