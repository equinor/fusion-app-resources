using System;

namespace Fusion.Resources.Domain
{
    public enum QueryTaskSource
    {
        ResourceOwner,
        TaskOwner
    }

    [Flags]
    public enum QueryTaskResponsible
    {
        ResourceOwner = 0b01,
        TaskOwner     = 0b10,
        Both          = 0b11
    }
}
