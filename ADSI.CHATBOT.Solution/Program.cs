using Amazon.SecretsManager;
using ChatBot.Application;
using ChatBot.Application.Configuration;
using ChatBot.Application.Configurations;
using ChatBot.Application.Interfaces.External;
using ChatBot.Infrastructure;
using ChatBot.Infrastructure.Services;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

// =============================
// AWS + Secrets
// =============================

builder.Services.Configure<AwsSettings>(
    configuration.GetSection("AWS"));

builder.Services.AddDefaultAWSOptions(configuration.GetAWSOptions());
builder.Services.AddAWSService<IAmazonSecretsManager>();
builder.Services.AddScoped<ISecretsManagerService, SecretsManagerService>();

var awsSettings = configuration.GetSection("AWS").Get<AwsSettings>();

if (awsSettings?.UseSecretsManager == true)
{
    using var tempProvider = builder.Services.BuildServiceProvider();
    using var scope = tempProvider.CreateScope();

    var secretService = scope.ServiceProvider
        .GetRequiredService<ISecretsManagerService>();

    var secretJson = await secretService.GetSecretAsync();
    var secretConfig = JsonSerializer.Deserialize<AwsSecretsConfig>(secretJson);

    configuration["ConnectionStrings:DefaultConnection"] =
        secretConfig!.ConnectionStrings.DefaultConnection;

    configuration["Tranxa:BaseUrl"] = secretConfig.Tranxa.BaseUrl;
    configuration["Tranxa:BaseUrlUltraRed"] = secretConfig.Tranxa.BaseUrlUltraRed;
    configuration["Tranxa:ClientId"] = secretConfig.Tranxa.ClientId;
    configuration["Tranxa:ClientSecret"] = secretConfig.Tranxa.ClientSecret;
    configuration["Tranxa:Scope"] = secretConfig.Tranxa.Scope;

    configuration["WhatsApp:AccessToken"] = secretConfig.WhatsApp.AccessToken;
    configuration["WhatsApp:PhoneNumberId"] = secretConfig.WhatsApp.PhoneNumberId;
    configuration["WhatsApp:VerifyToken"] = secretConfig.WhatsApp.VerifyToken;
}

// =============================
// Core Services
// =============================

builder.Services.AddApplication();
builder.Services.AddInfrastructure(configuration);

builder.Services.AddMemoryCache();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
    });

// =============================
// Health Check (BÁSICO SIN DB)
// =============================

builder.Services.AddHealthChecks();

// =============================
// Swagger SOLO Development
// =============================

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy => policy.AllowAnyOrigin()
                        .AllowAnyMethod()
                        .AllowAnyHeader());
});

var app = builder.Build();

// =============================
// Swagger solo en Development
// =============================

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// =============================
// Middleware
// =============================

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthorization();

app.MapControllers();

// =============================
// Health Endpoint
// =============================

app.MapHealthChecks("/health");

app.Run();