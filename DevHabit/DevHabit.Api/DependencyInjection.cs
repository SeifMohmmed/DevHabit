using Asp.Versioning;
using DevHabit.Api.Database;
using DevHabit.Api.DTOs.Habits;
using DevHabit.Api.Entities;
using DevHabit.Api.Middleware;
using DevHabit.Api.Services;
using DevHabit.Api.Services.Sorting;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Newtonsoft.Json.Serialization;
using Npgsql;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace DevHabit.Api;

/// <summary>
/// Centralized extension methods for registering application services.
/// This keeps Program.cs clean and organized.
/// </summary>
public static class DependencyInjection
{
    public static WebApplicationBuilder AddApiServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddControllers(options =>
        {
            // Return 406 if requested media type is not supported
            options.ReturnHttpNotAcceptable = true;
        })
        .AddNewtonsoftJson(options =>
        {
            // Use camelCase for JSON properties
            options.SerializerSettings.ContractResolver =
                new CamelCasePropertyNamesContractResolver();
        })
        // Enable XML format support
        .AddXmlSerializerFormatters();

        builder.Services.Configure<MvcOptions>(options =>
        {
            // Get JSON formatter
            var formatter = options.OutputFormatters
                .OfType<NewtonsoftJsonOutputFormatter>()
                .First();

            // Add custom HATEOAS media type globally
            formatter.SupportedMediaTypes.Add(
                CustomMediaTypeNames.Application.JsonV1);
            formatter.SupportedMediaTypes.Add(
                CustomMediaTypeNames.Application.JsonV2);
            formatter.SupportedMediaTypes.Add(
                CustomMediaTypeNames.Application.HateoasJson);
            formatter.SupportedMediaTypes.Add(
                CustomMediaTypeNames.Application.HateoasJsonV1);
            formatter.SupportedMediaTypes.Add(
                CustomMediaTypeNames.Application.HateoasJsonV2);
        });

        builder.Services
            .AddApiVersioning(options =>
            {
                // Sets default API version when client does not specify version
                options.DefaultApiVersion = new ApiVersion(1.0);

                // Automatically uses default version if version is missing from request
                options.AssumeDefaultVersionWhenUnspecified = true;

                // Adds supported API versions to response headers
                // Ex -> api-supported-versions : 1.0, 2.0 | api-deprecated-versions: 0.9
                options.ReportApiVersions = true;

                // Selects which API version should be used when multiple versions exist
                // Default selector chooses configured default version
                options.ApiVersionSelector = new DefaultApiVersionSelector(options);

                // Defines how API version is read from incoming requests
                options.ApiVersionReader = ApiVersionReader.Combine(

                    // Reads version from Accept header media type
                    // Ex -> Accept: application/json; v=1.0
                    new MediaTypeApiVersionReader(),

                    // Reads version from custom vendor media type
                    // Ex -> Accept: application/vnd.dev-habit.hateoas.v1+json
                    new MediaTypeApiVersionReaderBuilder()
                        .Template("application/vnd.dev-habit.hateoas.{version}+json")
                        .Build());
            })
            // Registers MVC services required for versioned controllers
            .AddMvc();

        // Adds OpenAPI/Swagger support
        builder.Services.AddOpenApi();

        return builder;
    }

    /// Configures global error handling and ProblemDetails responses.
    public static WebApplicationBuilder AddErrorHandling(this WebApplicationBuilder builder)
    {
        builder.Services.AddProblemDetails(options =>
        {
            options.CustomizeProblemDetails = context =>
            {
                // Add requestId to every error response for tracing/debugging
                context.ProblemDetails.Extensions.TryAdd(
                    "requestId",
                    context.HttpContext.TraceIdentifier);
            };
        });

        // Custom validation exception handler
        builder.Services.AddExceptionHandler<ValidationExceptionHandler>();

        // Global fallback exception handler
        builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

        return builder;
    }

    /// Configures Entity Framework Core with PostgreSQL.
    public static WebApplicationBuilder AddDatabase(this WebApplicationBuilder builder)
    {
        builder.Services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(
                builder.Configuration.GetConnectionString("Database"),
                npgsqlOptions =>
                    // Store EF migrations history in custom schema
                    npgsqlOptions.MigrationsHistoryTable(
                        HistoryRepository.DefaultTableName,
                        Schemas.Application))
            // Use snake_case naming convention for DB tables/columns
            .UseSnakeCaseNamingConvention());

        return builder;
    }

    public static WebApplicationBuilder AddObservability(this WebApplicationBuilder builder)
    {
        builder.Services.AddOpenTelemetry()
                    .ConfigureResource(resource =>
                        // Register service name in telemetry system
                        resource.AddService(builder.Environment.ApplicationName))
                    .WithTracing(tracing => tracing
                        // Trace outgoing HTTP calls
                        .AddHttpClientInstrumentation()
                        // Trace incoming ASP.NET Core requests
                        .AddAspNetCoreInstrumentation()
                        // Trace PostgreSQL queries
                        .AddNpgsql())
                    .WithMetrics(metrics => metrics
                        // Metrics for outgoing HTTP
                        .AddHttpClientInstrumentation()
                        // Metrics for incoming HTTP
                        .AddAspNetCoreInstrumentation()
                        // Runtime metrics (GC, CPU, etc.)
                        .AddRuntimeInstrumentation())
                    // Export telemetry using OTLP (e.g., to Jaeger, Grafana, etc.)
                    .UseOtlpExporter();

        builder.Logging.AddOpenTelemetry(options =>
        {
            options.IncludeScopes = true;
            options.IncludeFormattedMessage = true;
        });

        return builder;
    }

    /// Registers application-level services (business logic, helpers, etc.)
    public static WebApplicationBuilder AddApplicationServices(this WebApplicationBuilder builder)
    {
        // Enable DI validation during build (good practice in production apps)
        builder.Host.UseDefaultServiceProvider((context, options) =>
        {
            options.ValidateScopes = true;
            options.ValidateOnBuild = true;
        });

        // Automatically register all FluentValidation validators
        builder.Services.AddValidatorsFromAssemblyContaining<Program>();

        // Sorting infrastructure
        builder.Services.AddTransient<SortMappingProvider>();

        builder.Services.AddSingleton<ISortMappingDefinition,
            SortMappingDefinition<HabitDto, Habit>>(_ =>
                HabitMappings.SortMapping);

        // Data shaping service (used for selecting specific fields dynamically)
        builder.Services.AddTransient<DataShapingService>();

        // Allows services to access HttpContext
        builder.Services.AddHttpContextAccessor();

        // Service responsible for generating HATEOAS links
        builder.Services.AddTransient<LinkService>();

        return builder;
    }
}
