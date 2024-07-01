using System.Reflection;
using Fusion.AspNetCore.Api;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Microsoft.Extensions.DependencyInjection;

public class PatchPropertySchemaFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (context.Type.IsGenericType && context.Type.GetGenericTypeDefinition() == typeof(PatchProperty<>))
        {
            Type genericArgumentType = context.Type.GetGenericArguments()[0];
            OpenApiSchema genericArgumentSchema =
                context.SchemaGenerator.GenerateSchema(genericArgumentType, context.SchemaRepository);

            if (genericArgumentSchema.Reference == null)
            {
                CopySchemaProperties(schema, genericArgumentSchema);

                // Must override this for PatchProperties since it is not respected and all patch properties are nullable.
                schema.Nullable = true;
            }
            else
            {
                schema.Reference = genericArgumentSchema.Reference;
            }
        }
    }

    private void CopySchemaProperties(OpenApiSchema target, OpenApiSchema source)
    {
        foreach (PropertyInfo property in typeof(OpenApiSchema).GetProperties())
        {
            if (property.CanWrite)
            {
                property.SetValue(target, property.GetValue(source));
            }
        }
    }
}