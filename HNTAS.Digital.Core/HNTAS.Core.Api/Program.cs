using AutoMapper.Internal;
using FluentValidation;
using HNTAS.Core.Api.Configuration;
using HNTAS.Core.Api.DataMigrations;
using HNTAS.Core.Api.Interfaces;
using HNTAS.Core.Api.MappingProfiles;
using HNTAS.Core.Api.Services;
using HNTAS.Core.Api.Validators.Arms;
using HNTAS.Core.Api.Validators.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.Configure<NotificationSettings>(
    builder.Configuration.GetSection("NotificationSettings"));
// Configure db settings
builder.Services.Configure<AWSDocDbSettings>(
    builder.Configuration.GetSection("AWSDocDbSettings"));

builder.Services.Configure<HntasServiceSettings>(
    builder.Configuration.GetSection("HntasService"));

builder.Configuration
    .AddJsonFile("units.json", optional: false, reloadOnChange: true);

builder.Services.Configure<UnitSettings>(builder.Configuration);

builder.Services.AddControllers()
.ConfigureApiBehaviorOptions(options =>
{
    options.ClientErrorMapping[StatusCodes.Status400BadRequest].Link = null;
    options.ClientErrorMapping[StatusCodes.Status404NotFound].Link = null;
    options.ClientErrorMapping[StatusCodes.Status500InternalServerError].Link = null;
    options.ClientErrorMapping[StatusCodes.Status503ServiceUnavailable].Link = null;
    // This stops the RFC link from appearing for 400 errors
});

// Register AutoMapper, scan for profiles, and apply global recursion protection
builder.Services.AddAutoMapper(cfg =>
{
    cfg.AddMaps(typeof(UserMappingProfile).Assembly);

    // Mitigate CVE-2026-32933 by forcing a max depth limit across all mappings
    cfg.Internal().ForAllMaps((_, mapExpr) =>
    {
        mapExpr.MaxDepth(64);
    });
});

builder.Services.AddSingleton<IUserService, UserService>();
builder.Services.AddSingleton<IOrganisationService, OrganisationService>();
builder.Services.AddScoped<IInvitationService, InvitationService>();
builder.Services.AddSingleton<ICounterService, CounterService>();
builder.Services.AddSingleton<ISoaService, SoaService>();
builder.Services.AddSingleton<IGovUkNotifyService, GovUkNotifyService>();
builder.Services.AddSingleton<IHeatNetworkService, HeatNetworkService>();
builder.Services.AddSingleton<IEmailService, EmailService>();
builder.Services.AddScoped<IFeedbackService, FeedbackService>();
builder.Services.AddSingleton<ICountryAndTerritoryService, CountryAndTerritoryService>();
builder.Services.AddSingleton<IAssessorService, AssessorService>();
builder.Services.AddSingleton<IAuditService, AuditService>();
builder.Services.AddScoped<IArmsKpiService, ArmsKpiService>();
builder.Services.AddScoped<INotificationHistoryService, NotificationHistoryService>();
builder.Services.AddScoped<IKpiRuleValidator, KpiRuleValidator>();
builder.Services.AddScoped<IHeatNetworkValidator, HeatNetworkValidator>();
builder.Services.AddScoped<IKpiSubmissionAuditService, KpiSubmissionAuditService>();
builder.Services.AddScoped<IUserStatsService, UserStatsService>();
builder.Services.AddScoped<ISuperUserService, SuperUserService>();
builder.Services.AddScoped<IArmsPowerBiService, ArmsPowerBiService>();
builder.Services.AddSingleton<IUnitService, UnitService>();

//Data Migrations
builder.Services.AddScoped<IDataMigration, SeedCountriesAndTerritories>();
builder.Services.AddScoped<IDataMigration, KpiSeedData>();
builder.Services.AddScoped<IDataMigration, SeedAssessors>();

builder.Services.Configure<AWSDocDbSettings>(
    builder.Configuration.GetSection("AWSDocDbSettings"));

builder.Services.Configure<ArmsSettings>(
    builder.Configuration.GetSection("ArmsSettings"));

builder.Services.AddSingleton<IMongoClient>(sp =>
{
    var connectionString = Environment.GetEnvironmentVariable("DOCUMENT_DB_CONNECTION_STRING");
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        throw new InvalidOperationException("DOCUMENT_DB_CONNECTION_STRING environment variable is not set.");
    }

    return new MongoClient(connectionString); // Only ONE client instance created
});

builder.Services.AddSingleton<IMongoDatabase>(sp =>
{
    var client = sp.GetRequiredService<IMongoClient>();
    var dbSettings = sp.GetRequiredService<IOptions<AWSDocDbSettings>>().Value;

    return client.GetDatabase(dbSettings.DatabaseName);
});


builder.Services.AddSingleton<INotificationClientWrapper>(sp =>
{
    var apiKey = Environment.GetEnvironmentVariable("GOV_NOTIFY_API_KEY");

    if (string.IsNullOrEmpty(apiKey))
        throw new InvalidOperationException("GOV_NOTIFY_API_KEY is not configured.");

    return new NotificationClientService(apiKey);
});



builder.Services.AddHttpClient();
builder.Services.AddScoped<ICarbonCalculatorService, CarbonCalculatorService>();
builder.Services.AddScoped<ICsvImportService, CsvImportService>();

builder.Services.AddScoped<ISubmissionCCService, SubmissionCCService>();
builder.Services.AddScoped<ICarbonCalculatorRuleValidation, CarbonCalculatorRuleValidation>();

// Register FluentValidation validators
builder.Services.AddValidatorsFromAssemblyContaining<KpiSubmissionRequestValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<KpiSubmissionRequestV2Validator>();

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
});

builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var problemErrors = new Dictionary<string, string[]>();

            foreach (var entry in context.ModelState)
            {
                // 1. Skip the internal "request" parameter error
                if (entry.Key.Equals("request", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var errorMessages = entry.Value.Errors.Select(e =>
                {
                    // 2. Handle the Unmapped Member error (as we did before)
                    if (e.ErrorMessage.Contains("could not be mapped to any .NET member"))
                    {
                        return "Unknown or unexpected property detected in the JSON request.";
                    }
                    return e.ErrorMessage;
                }).ToArray();

                if (errorMessages.Any())
                {
                    // 3. Clean up the key names (e.g., "meta_data.prop" instead of "$.meta_data.prop")
                    var cleanKey = entry.Key.Replace("$.", "");
                    problemErrors.Add(cleanKey, errorMessages);
                }
            }

            // 4. If the only error was a missing body, provide a clear top-level message
            if (!problemErrors.Any() && !context.ModelState.IsValid)
            {
                return new BadRequestObjectResult(new ProblemDetails
                {
                    Title = "Bad Request",
                    Status = StatusCodes.Status400BadRequest,
                    Detail = "The request body is missing, empty, or not a valid JSON object."
                });
            }

            var problemDetails = new ValidationProblemDetails(problemErrors)
            {
                Title = "Invalid Request Structure",
                Status = StatusCodes.Status400BadRequest,
                Detail = "The submission contains properties that are not part of the HNTAS schema.",
                Instance = context.HttpContext.Request.Path
            };

            return new BadRequestObjectResult(problemDetails);
        };
    });

// Ensure FluentValidation uses JSON property names in error messages
builder.Services.UseJsonPropertyNames();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi("HNTAS.Core.Api");


var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var serviceProvider = scope.ServiceProvider;

    var awsDocDbSettings = serviceProvider.GetRequiredService<IOptions<AWSDocDbSettings>>();
    var logger = serviceProvider.GetRequiredService<ILogger<SeedCountriesAndTerritories>>();

    var migrations = serviceProvider.GetServices<IDataMigration>();

    foreach (var migration in migrations)
    {
        try
        {
            await migration.RunAsync();
        }
        catch (Exception ex)
        {
            var prologger = serviceProvider.GetRequiredService<ILogger<Program>>();
            prologger.LogError(ex, "Error running migration: {MigrationName}", migration.GetType().Name);
            throw;
        }
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/HNTAS.Core.Api.json", "HNTAS Core API v1");
    });
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
