using System;

namespace Fusion.Resources.Api.Controllers
{
    public class PersonReference
    {
        public Guid? AzureUniquePersonId { get; set; }
        public string Mail { get; set; }
    }
}
