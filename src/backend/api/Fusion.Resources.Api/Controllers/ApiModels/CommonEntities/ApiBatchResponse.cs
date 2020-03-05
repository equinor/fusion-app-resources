using System.Collections.Generic;

namespace Fusion.Resources.Api.Controllers
{
    public class ApiBatchResponse<T> : List<ApiBatchItemResponse<T>>
    {
        public ApiBatchResponse(IEnumerable<ApiBatchItemResponse<T>> items) : base(items)
        {
        }
    }


}
