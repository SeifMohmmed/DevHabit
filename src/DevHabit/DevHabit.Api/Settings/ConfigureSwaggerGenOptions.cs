using System.Reflection;
using Asp.Versioning.ApiExplorer;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace DevHabit.Api.Settings;

public sealed class ConfigureSwaggerGenOptions(
    IApiVersionDescriptionProvider apiVersionDescriptionProvider) : IConfigureOptions<SwaggerGenOptions>
{
    public void Configure(SwaggerGenOptions options)
    {
        foreach (ApiVersionDescription description in apiVersionDescriptionProvider.ApiVersionDescriptions)
        {

            var openApiInfo = new OpenApiInfo
            {
                Title = $"DevHabit.Api v{description.ApiVersion}",
                Version = description.ApiVersion.ToString()
            };
            options.SwaggerDoc(description.GroupName, openApiInfo);
        }

        options.ResolveConflictingActions(description => description.First());

        string xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
        string xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);

        options.IncludeXmlComments(xmlPath);

        options.CustomSchemaIds(type => type.FullName?.Replace("+", "."));
        options.DescribeAllParametersInCamelCase();
    }

    public void Configure(string? name, SwaggerGenOptions options)
    {
        throw new NotImplementedException();
    }
}
