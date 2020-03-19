using System;

namespace Fusion.Resources
{
    public class RequestAlreadyExistsError : InvalidOperationException
    {
        public RequestAlreadyExistsError(string message) : base(message)
        {

        }
    }
}
