using System;
using System.Threading.Tasks;
using Fusion.Integration;
using Fusion.Integration.Notification;
using Fusion.Integration.Profile;

namespace Fusion.Resources.Api.Tests.FusionMocks
{
    internal class TestBuilderUtils : IBuilderUtils
    {
        private readonly IBuilderUtils builderUtils;

        public TestBuilderUtils(IBuilderUtils builderUtils)
        {
            this.builderUtils = builderUtils;
        }

        public string FormatDateString(DateTime? date) => builderUtils.FormatDateString(date);

        public string FormatDateString(DateTimeOffset? date) => builderUtils.FormatDateString(date);

        public string FormatTimeString(DateTime? date) => builderUtils.FormatDateString(date);

        public string FormatTimeString(DateTimeOffset? date) => builderUtils.FormatDateString(date);

        public Task<Base64ImageUrl> GetAppIconAsync(string appKey) => builderUtils.GetAppIconAsync(appKey);

        public Task<Base64ImageUrl> GetProfileImageAsync(PersonIdentifier personIdentifier) => builderUtils.GetProfileImageAsync(personIdentifier);

        public Task<Base64ImageUrl?> ResolveAppIconAsync(string appKey) => builderUtils.ResolveAppIconAsync(appKey);

        public Task<Uri> ResolvePortalHostAsync() => Task.FromResult(new Uri("https://localhost"));

        public Task<FusionPersonProfile> ResolveProfileAsync(PersonIdentifier personIdentifier) => builderUtils.ResolveProfileAsync(personIdentifier);

        public Task<Base64ImageUrl?> ResolveProfileImageAsync(PersonIdentifier personIdentifier) => builderUtils.ResolveProfileImageAsync(personIdentifier);

        public Task<ProfileReference?> ResolveProfileReferenceAsync(PersonIdentifier personIdentifier) => builderUtils.ResolveProfileReferenceAsync(personIdentifier);

        public void SetLocale(DesignerLocale locale)
        {
            throw new NotImplementedException();
        }
    }
}