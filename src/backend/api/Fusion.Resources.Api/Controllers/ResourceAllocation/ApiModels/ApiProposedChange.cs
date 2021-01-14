using Fusion.Resources.Domain;

namespace Fusion.Resources.Api.Controllers
{
    public class ApiProposedChange
    {
        public ApiProposedChange(QueryProposedChange query)
        {
            this.Prop = query.Prop;
            this.Value = query.Value;
        }

        public string Prop { get; set; }
        public string Value { get; set; }
    }
}