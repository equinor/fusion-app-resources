using System;

namespace Fusion.Resources.Domain
{
    public static class DepartmentUtils
    {
        public static int GetLevel(string department) => department.Split(" ").Length;

        public static bool IsRelevantBasePositionDepartment(int level, string sourceDepartment, string basePositionDepartment)
        {
            if (sourceDepartment is null || basePositionDepartment is null)
                return false;

            var bpLevel = basePositionDepartment.Split(" ").Length;
            var levelDelta = Math.Abs(bpLevel - level);

            if (levelDelta <= 2 && sourceDepartment.StartsWith(basePositionDepartment, StringComparison.OrdinalIgnoreCase))
                return true;

            if (basePositionDepartment.StartsWith(sourceDepartment, StringComparison.OrdinalIgnoreCase))
                return true;

            return false;
        }
    }
}
