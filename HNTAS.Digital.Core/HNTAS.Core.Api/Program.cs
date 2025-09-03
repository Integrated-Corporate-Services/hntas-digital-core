using HNTAS.Core.Api.Configuration;
using HNTAS.Core.Api.Interfaces;
using HNTAS.Core.Api.MappingProfiles;
using HNTAS.Core.Api.Services;

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
builder.Services.AddSingleton<IGovUkNotifyService, GovUkNotifyService>();
builder.Services.AddSingleton<IHeatNetworkService, HeatNetworkService>();
builder.Services.AddSingleton<IEmailService, EmailService>();

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
});
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi("HNTAS.Core.Api");

Console.WriteLine("***********************************");
Console.WriteLine("Environment: " + builder.Environment.EnvironmentName);

var app = builder.Build();

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
