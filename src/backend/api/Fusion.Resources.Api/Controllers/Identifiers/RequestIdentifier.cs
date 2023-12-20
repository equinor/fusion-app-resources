using System;
using System.Text.Json.Serialization;
using Azure.Core;
using Microsoft.AspNetCore.Mvc;

namespace Fusion.Resources.Api.Controllers
{
    [ModelBinder(BinderType = typeof(RequestIdentifierResolver))]
    public class RequestIdentifier
    {
        public RequestIdentifier(string originalIdentifier, Guid requestId, long number)
        {
            OriginalIdentifier = originalIdentifier;
            RequestId = requestId;
            Number = number;
        }

        private RequestIdentifier(string originalIdentifier)
        {
            OriginalIdentifier = originalIdentifier;
        }

        /// <summary>
        /// The original identifier passed by user.
        /// 
        /// Readonly, entity is cached and reused.
        /// </summary>
        [JsonIgnore]
        public string OriginalIdentifier { get; }

        /// <summary>
        /// The request number if request exists.
        /// 
        /// Readonly, entity is cached and reused.
        /// </summary>
        [JsonIgnore]
        public long? Number { get; }


        /// <summary>
        /// The request id, if the request exists.
        /// 
        /// Readonly, entity is cached and reused.
        /// </summary>
        [JsonIgnore]
        public Guid? RequestId { get; }

        /// <summary>
        /// Force the id. 
        /// Either return the actual resovled id or try and parse the provided id.
        /// </summary>
        [JsonIgnore]
        public Guid Id => RequestId.HasValue ? RequestId.Value : Guid.TryParse(OriginalIdentifier, out Guid providedId) ? providedId : throw new NullReferenceException("Trying to access id of resolved request, but request does not exist.");

        [JsonIgnore]
        public bool Exists => RequestId.HasValue;

        
        public static implicit operator Guid(RequestIdentifier identifier)
        {
            return identifier.Id;
        }

        public static RequestIdentifier NotFound(string identifier) => new RequestIdentifier(identifier);

        internal ActionResult NotFoundResult()
        {
            return ApiErrors.NotFound($"Could not locate request. Can use either request id or number.", OriginalIdentifier);
        }
    }
}
