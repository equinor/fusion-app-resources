using System;

namespace Fusion.Resources.Logic.Commands
{
    public partial class ResourceAllocationRequest
    {
        public partial class ResourceOwner
        {
            private struct SubType
            {
                public enum Types { Adjustment, ChangeResource, RemoveResource }

                public SubType(string? type)
                {
                    Value = type is null ? Types.Adjustment : Enum.Parse<Types>(type, true);
                }

                public Types Value { get; set; }
            }
        }
    }
}
