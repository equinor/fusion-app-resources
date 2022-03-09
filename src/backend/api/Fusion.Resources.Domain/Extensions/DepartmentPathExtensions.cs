using Fusion.Resources.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fusion.Resources
{
    public static class DepartmentPathExtensions
    {
        /// <summary>
        /// Returns all parent department paths, excluding the current department. They are returned in the order of neares parent first, top level last.
        /// </summary>
        /// <param name="path">Department to generate parents for</param>
        /// <returns></returns>
        public static IEnumerable<string> GetAllParents(this DepartmentPath path)
        {
            var parent = path.Parent();

            while (!string.IsNullOrWhiteSpace(parent))
            {
                yield return parent;

                path = new DepartmentPath(parent);
                parent = path.Parent();
            }
        }
    }
}
