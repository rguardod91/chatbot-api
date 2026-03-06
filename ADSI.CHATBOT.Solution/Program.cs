using Amazon.SecretsManager;
using ChatBot.Application;
using ChatBot.Application.Configuration;
using ChatBot.Application.Configurations;
using ChatBot.Application.Interfaces.External;
using ChatBot.Infrastructure;
using ChatBot.Infrastructure.Persistence.Context;
using ChatBot.Infrastructure.Services;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;
var environment = builder.Environment;

// =============================
// AWS + Secrets
// =============================

builder.Services.Configure<AwsSettings>(
    configuration.GetSection("AWS"));

builder.Services.AddDefaultAWSOptions(configuration.GetAWSOptions());
builder.Services.AddAWSService<IAmazonSecretsManager>();
builder.Services.AddScoped<ISecretsManagerService, SecretsManagerService>();

var awsSettings = configuration.GetSection("AWS").Get<AwsSettings>();

if (!environment.IsDevelopment() && awsSettings?.UseSecretsManager == true)
{
    Console.WriteLine("🌐 AWS environment detected. Loading secrets...");

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
else
{
    Console.WriteLine("💻 Local environment detected.");
}

// 🔎 LOG DEL CONNECTION STRING
Console.WriteLine("===== DATABASE CONNECTION STRING =====");
Console.WriteLine(configuration.GetConnectionString("DefaultConnection"));
Console.WriteLine("=======================================");

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

builder.Services.AddHealthChecks();

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

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// 🔎 TEST DE CONEXIÓN A BASE
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<TranxaDbContext>();

    Console.WriteLine("===== TESTING DATABASE CONNECTION =====");

    var canConnect = context.Database.CanConnect();

    Console.WriteLine($"Database connection OK: {canConnect}");

    Console.WriteLine("=======================================");
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();