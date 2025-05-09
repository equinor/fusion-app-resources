using Swashbuckle.AspNetCore.SwaggerGen;
using System.ComponentModel;

namespace Microsoft.Extensions.DependencyInjection
{
    public class FusionSwaggerConfig
    {
        internal static bool UseFusionSwaggerSetup = false;

        internal Action<SwaggerGenOptions>? SetupAction { get; private set; }

        internal string Description { get; private set; } = string.Empty;

        internal FusionSwaggerConfig()
        {
            UseFusionSwaggerSetup = true;
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

        public FusionSwaggerConfig AddDescription(string description)
        {
            Description = description;
            return this;
        }
    }
}
