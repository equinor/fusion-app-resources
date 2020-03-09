using Fusion.Integration.Configuration;
using Fusion.Resources.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fusion.Resources.Api.Authentication
{
    public class SqlTokenProvider : ISqlTokenProvider
    {
        private readonly IFusionTokenProvider fusionTokenProvider;

        public SqlTokenProvider(IFusionTokenProvider fusionTokenProvider)
        {
            this.fusionTokenProvider = fusionTokenProvider;
        }

        public Task<string> GetAccessTokenAsync()
        {
            return fusionTokenProvider.GetApplicationTokenAsync("https://database.windows.net/");
        }
    }
}
