using System;
using System.Collections.Generic;
using System.Text;

namespace Fusion.Resources
{
    public class IntegrationError : Exception
    {
        public IntegrationError(string message, Exception inner) : base(message, inner)
        {
        }
    }
}
