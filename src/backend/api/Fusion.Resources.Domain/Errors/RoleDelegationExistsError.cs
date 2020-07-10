using System;

namespace Fusion.Resources
{
    public class RoleDelegationExistsError : Exception
    {
        public RoleDelegationExistsError() : base("The person already have the role")
        {
        }
    }
}
