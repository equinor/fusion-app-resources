using System;

namespace Fusion.Resources.Api.Controllers
{
    public class ProjectBinderError : Exception
    {
        public ProjectBinderError(string message) : base(message)
        {
        }
        public ProjectBinderError(string message, Exception ex) : base(message, ex)
        {
        }
    }


}
