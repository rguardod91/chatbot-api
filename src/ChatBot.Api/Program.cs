using ChatBot.Application;
using ChatBot.Infrastructure;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

var configuration = builder.Configuration;

// Controllers
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
    });

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "ChatBot API",
        Version = "v1",
        Description = "WhatsApp Banking ChatBot"
    });
});

// Memory Cache
builder.Services.AddMemoryCache();

// Health Checks
builder.Services.AddHealthChecks();

// DI
builder.Services.AddApplication();
builder.Services.AddInfrastructure(configuration);

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy => policy.AllowAnyOrigin()
                        .AllowAnyMethod()
                        .AllowAnyHeader());
});

var app = builder.Build();

// Swagger solo en Development (recomendado)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("AllowAll");

app.UseAuthorization();

app.MapControllers();

// Health endpoint requerido por AWS
app.MapHealthChecks("/health");

app.Run();