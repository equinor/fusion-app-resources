using Fusion.ApiClients.Org;
using System;
using System.Linq;

namespace Fusion.Resources.Domain
{
    public static class OrgApiModelExtensions
    {
        public static T TryGetSettingOrDefault<T>(this ApiBasePositionV2 basePosition, string settingsKey, T defaultValue)
        {
            if (basePosition.Settings is null || !basePosition.Settings.Keys.Any(k => string.Equals(settingsKey, k, StringComparison.OrdinalIgnoreCase)))
                return defaultValue;

            var kv = basePosition.Settings.First(kv => string.Equals(kv.Key, settingsKey, StringComparison.OrdinalIgnoreCase));

            try
            {
                return (T)kv.Value;
            }
            catch
            {
                return defaultValue;
            }
        }

        /// <summary>
        /// The base position has the 'requireDirectRequest' setting set to true.
        /// </summary>
        public static bool RequiresDirectRequest(this ApiBasePositionV2 basePosition)
        {
            return basePosition?.TryGetSettingOrDefault<bool?>("requireDirectRequest", null) == true;
        }
    }
}
