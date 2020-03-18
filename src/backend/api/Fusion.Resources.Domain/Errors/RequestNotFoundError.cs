using System;
using System.Collections.Generic;
using System.Text;

namespace Fusion.Resources
{
    public class RequestNotFoundError : ArgumentException
    {
        public RequestNotFoundError(Guid requestId) : base ($"Could not locate request by id '{requestId}'")
        {
            RequestId = requestId;
        }

        public Guid RequestId { get; }
    }
}
