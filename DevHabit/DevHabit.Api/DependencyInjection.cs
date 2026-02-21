using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Asp.Versioning;
using DevHabit.Api.Common.Auth;
using DevHabit.Api.Database;
using DevHabit.Api.DTOs.Habits;
using DevHabit.Api.Entities;
using DevHabit.Api.Jobs;
using DevHabit.Api.Middleware;
using DevHabit.Api.Services;
using DevHabit.Api.Services.Sorting;
using DevHabit.Api.Settings;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json.Serialization;
using Npgsql;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Quartz;
using Refit;

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
                        .Template("application/vnd.dev-habit.hateoas.v{version}+json")
                        .Build());
            })
            // Registers MVC services required for versioned controllers
            .AddMvc();

        // Adds OpenAPI/Swagger support
        builder.Services.AddOpenApi();

        builder.Services.AddResponseCaching();

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

        builder.Services.AddDbContext<ApplicationIdentityDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("Database"),
        npgsqlOptions =>
            // Store EF migrations history in custom schema
            npgsqlOptions.MigrationsHistoryTable(
                HistoryRepository.DefaultTableName,
                Schemas.Identity))
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

        // Adds OpenTelemetry logging to capture structured logs
        builder.Logging.AddOpenTelemetry(options =>
        {
            // Includes logging scopes (useful for request tracing and correlation IDs)
            options.IncludeScopes = true;

            // Includes the fully formatted log message instead of only template + parameters
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

        // This allows the API to translate client sort parameters (DTO fields)
        // into corresponding entity properties safely.
        // Ex: ?sort=name → maps to Habit.Name
        builder.Services.AddSingleton<ISortMappingDefinition,
            SortMappingDefinition<HabitDto, Habit>>(_ =>
                HabitMappings.SortMapping);

        // Data shaping service (used for selecting specific fields dynamically)
        builder.Services.AddTransient<DataShapingService>();

        // Allows services to access HttpContext
        builder.Services.AddHttpContextAccessor();

        // Service responsible for generating HATEOAS links
        builder.Services.AddTransient<LinkService>();

        // Service responsible for Token generator
        builder.Services.AddTransient<TokenProvider>();

        // Registers in-memory caching service for the application
        builder.Services.AddMemoryCache();

        // Registers UserContext as scoped (one instance per HTTP request)
        builder.Services.AddScoped<UserContext>();

        // Registers GitHub API service
        builder.Services.AddTransient<GitHubService>();

        builder.Services.AddTransient<RefitGitHubService>();

        // Service responsible for managing GitHub tokens in DB
        builder.Services.AddScoped<GitHubAccessTokenService>();

        // Named HttpClient configured for GitHub API
        builder.Services.AddHttpClient("github")
            .ConfigureHttpClient(client =>
            {
                client.BaseAddress = new("https://api.github.com");

                // Required User-Agent header for GitHub API
                client.DefaultRequestHeaders
                .UserAgent.Add(new("DevHabit", "1.0"));

                // Accept GitHub JSON format
                client.DefaultRequestHeaders
                .Accept.Add(new("application/vnd.github+json"));
            });

        builder.Services
            .AddRefitClient<IGitHubApi>(new RefitSettings
            {
                ContentSerializer = new NewtonsoftJsonContentSerializer()
            })
            .ConfigureHttpClient(client => client.BaseAddress = new Uri("https://api.github.com"));

        // Bind EncryptionOptions from configuration
        builder.Services.Configure<EncryptionOptions>(builder.Configuration.GetSection("Encryption"));

        // Encryption service for token protection
        builder.Services.AddTransient<EncryptionService>();

        // Registers the in-memory ETag store as a singleton
        // so all requests share the same cache instance
        builder.Services.AddSingleton<InMemoryETagStore>();

        return builder;
    }

    /// <summary>
    /// Configures ASP.NET Core Identity + JWT authentication.
    /// </summary>
    public static WebApplicationBuilder AddAuthenticationService(this WebApplicationBuilder builder)
    {
        builder.Services
            // Registers ASP.NET Core Identity with default user & role entities
            .AddIdentity<IdentityUser, IdentityRole>()
            // Configures EF Core store for Identity
            .AddEntityFrameworkStores<ApplicationIdentityDbContext>();

        // Bind Jwt settings from configuration (appsettings.json → Jwt section)
        builder.Services.Configure<JwtAuthOptions>(
            builder.Configuration.GetSection("Jwt"));

        // Retrieve strongly typed Jwt settings
        JwtAuthOptions jwtAuthOptions =
            builder.Configuration.GetSection("Jwt").Get<JwtAuthOptions>()!;

        builder.Services
            // Configures authentication services and sets default schemes
            // DefaultAuthenticateScheme → used when validating incoming requests
            // DefaultChallengeScheme → used when returning 401 Unauthorized responses
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            // Registers JWT Bearer authentication handler
            // This enables the API to accept tokens via:
            // Authorization: Bearer {token}
            .AddJwtBearer(options =>
            {
                // Prevent automatic mapping of claim types to Microsoft defaults
                options.MapInboundClaims = false;

                options.TokenValidationParameters =
                    new TokenValidationParameters
                    {
                        // Validates the token issuer to ensure it was generated by trusted authority
                        ValidIssuer = jwtAuthOptions.Issuer,

                        // Validates the intended audience of the token
                        ValidAudience = jwtAuthOptions.Audience,

                        // Validates token signature using symmetric security key
                        // Prevents tampering with token payload
                        IssuerSigningKey = new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(jwtAuthOptions.Key)),

                        // Enable signature validation
                        ValidateIssuerSigningKey = true,

                        // Set which claim represents the user's name
                        NameClaimType = JwtRegisteredClaimNames.Email,

                        // Set which claim represents user roles
                        RoleClaimType = JwtCustomClaimNames.Role
                    };
            });

        // Enables authorization policies
        builder.Services.AddAuthorization();

        return builder;
    }

    public static WebApplicationBuilder AddBackgroundJobs(this WebApplicationBuilder builder)
    {
        // Configure Quartz scheduler
        builder.Services.AddQuartz(configurator =>
        {
            // Register scheduler job
            configurator.AddJob<GitHubAutomationSchedulerJob>(options =>
                options.WithIdentity("github-automation-scheduler"));

            // Configure trigger for scheduler job
            configurator.AddTrigger(options =>
            {
                options.ForJob("github-automation-scheduler")
                    .WithIdentity("github-automation-scheduler-trigger")
                    .WithSimpleSchedule(scheduleBuilder =>
                    {
                        // Load scan interval from configuration
                        GitHubAutomationOptions settings = builder.Configuration
                            .GetSection(GitHubAutomationOptions.SectionName)
                            .Get<GitHubAutomationOptions>()!;

                        // Run job repeatedly based on configured interval
                        scheduleBuilder.WithIntervalInMinutes(settings.ScanIntervalInMinutes)
                            .RepeatForever();
                    });
            });
        });

        // Register Quartz hosted service
        builder.Services.AddQuartzHostedService(opt => opt.WaitForJobsToComplete = true);

        return builder;
    }

    public static WebApplicationBuilder AddCorsPolicy(this WebApplicationBuilder builder)
    {
        // Load CORS settings from configuration
        CorsOptions corsOptions = builder.Configuration
            .GetSection(CorsOptions.SectionName)
            .Get<CorsOptions>()!;

        builder.Services.AddCors(options =>
        {
            options.AddPolicy(CorsOptions.PolicyName, policy =>
            {
                // Allow configured origins
                policy
                    .WithOrigins(corsOptions.AllowedOrigins)
                    .AllowAnyMethod()
                    .AllowAnyHeader();
            });
        });

        return builder;
    }
}
