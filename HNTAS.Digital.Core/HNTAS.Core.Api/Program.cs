using FluentValidation;
using HNTAS.Core.Api.Configuration;
using HNTAS.Core.Api.DataMigrations;
using HNTAS.Core.Api.Interfaces;
using HNTAS.Core.Api.MappingProfiles;
using HNTAS.Core.Api.Services;
using HNTAS.Core.Api.Validators.Arms;
using HNTAS.Core.Api.Validators.Extensions;
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

// Register AutoMapper and scan for profiles
builder.Services.AddAutoMapper(typeof(UserMappingProfile));

builder.Services.AddSingleton<IUserService, UserService>();
builder.Services.AddSingleton<IOrganisationService, OrganisationService>();
builder.Services.AddSingleton<IInvitationService, InvitationService>();
builder.Services.AddSingleton<ICounterService, CounterService>();
builder.Services.AddSingleton<ISoaService, SoaService>();
builder.Services.AddSingleton<IGovUkNotifyService, GovUkNotifyService>();
builder.Services.AddSingleton<IHeatNetworkService, HeatNetworkService>();
builder.Services.AddSingleton<IEmailService, EmailService>();
builder.Services.AddSingleton<ICountryAndTerritoryService, CountryAndTerritoryService>();
builder.Services.AddSingleton<IAssessorService, AssessorService>();
builder.Services.AddSingleton<IAuditService, AuditService>();
builder.Services.AddScoped<IArmsKpiService, ArmsKpiService>();


builder.Services.Configure<AWSDocDbSettings>(
    builder.Configuration.GetSection("AWSDocDbSettings"));

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


builder.Services.AddHttpClient();
builder.Services.AddScoped<ICarbonCalculatorService, CarbonCalculatorService>();
builder.Services.AddScoped<ICsvImportService, CsvImportService>();

builder.Services.AddScoped<IHNDataImportExportService, HNDataImportExportService>();

// Register FluentValidation validators
builder.Services.AddValidatorsFromAssemblyContaining<KpiSubmissionRequestValidator>();

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
});

// Ensure FluentValidation uses JSON property names in error messages
builder.Services.UseJsonPropertyNames();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi("HNTAS.Core.Api");

Console.WriteLine("***********************************");
Console.WriteLine("Environment: " + builder.Environment.EnvironmentName);

string? regEnabled = Environment.GetEnvironmentVariable("IS_REGISTRATION_ENABLED");

Console.WriteLine($"--- Startup Debug ---");
Console.WriteLine($"Raw Env Var 'IS_REGISTRATION_ENABLED': {regEnabled ?? "NOT FOUND"}");
Console.WriteLine($"---------------------");

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var serviceProvider = scope.ServiceProvider;

    var awsDocDbSettings = serviceProvider.GetRequiredService<IOptions<AWSDocDbSettings>>();
    var logger = serviceProvider.GetRequiredService<ILogger<SeedCountriesAndTerritories>>();

    var migration = new SeedCountriesAndTerritories(awsDocDbSettings, logger);
    await migration.RunAsync();
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
