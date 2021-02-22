using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace Microsoft.Extensions.DependencyInjection
{
    public class FusionSwaggerConfig
    {
        internal static bool UseFusionSwaggerSetup = false;

        internal Action<SwaggerGenOptions>? SetupAction { get; private set; }
        internal List<int> EnabledVersions { get; } = new List<int>();
        internal bool AddPreviewEndpoints { get; private set; }

        internal FusionSwaggerConfig()
        {
            UseFusionSwaggerSetup = true;
        }


        public FusionSwaggerConfig AddApiVersion(int majorVersion)
        {
            EnabledVersions.Add(majorVersion);
            return this;
        }

        public FusionSwaggerConfig AddApiPreview()
        {
            AddPreviewEndpoints = true;
            return this;
        }

        public FusionSwaggerConfig ConfigureSwaggerGen(Action<SwaggerGenOptions> swaggerSetup)
        {
            SetupAction = swaggerSetup;
            return this;
        }

        public FusionSwaggerConfig ForceStringConverter<TModel>()
        {
            TypeConverterAttribute typeConverterAttribute = new TypeConverterAttribute(typeof(ToStringTypeConverter));
            TypeDescriptor.AddAttributes(typeof(TModel), typeConverterAttribute);

            return this;
        }
    }
}
