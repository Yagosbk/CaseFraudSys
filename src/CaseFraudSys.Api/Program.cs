using System.Diagnostics;
using Amazon;
using Amazon.DynamoDBv2;
using CaseFraudSys.Api.Application.Services;
using CaseFraudSys.Api.Domain.Repositories;
using CaseFraudSys.Api.Infrastructure.DynamoDb;
using CaseFraudSys.Api.Infrastructure.Middleware;
using CaseFraudSys.Api.Infrastructure.Swagger;
using CaseFraudSys.Api.Infrastructure.Utils;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var firstError = context.ModelState
                .Where(e => e.Value?.Errors.Count > 0)
                .Select(e => e.Value!.Errors[0].ErrorMessage)
                .FirstOrDefault() ?? "Requisição inválida.";

            var traceId = Activity.Current?.Id ?? context.HttpContext.TraceIdentifier;
            return new BadRequestObjectResult(ApiResponse.Fail(firstError, traceId));
        };
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerDocumentation();

builder.Services.AddScoped<IAccountLimitRepository, DynamoDbAccountLimitRepository>();
builder.Services.AddScoped<IPixIdempotencyRepository, DynamoDbPixIdempotencyRepository>();
builder.Services.AddScoped<IAccountLimitService, AccountLimitService>();
builder.Services.AddScoped<IPixTransactionService, PixTransactionService>();

// DynamoDB Local
builder.Services.AddSingleton<IAmazonDynamoDB>(_ =>
{
    var config = new AmazonDynamoDBConfig
    {
        RegionEndpoint = RegionEndpoint.USEast1,
        ServiceURL = builder.Configuration["AWS:ServiceURL"]
    };
    return new AmazonDynamoDBClient("local", "local", config);
});

builder.Services.AddSingleton<DynamoDbTableInitializer>();
builder.Services.AddSingleton<DynamoDbDataSeeder>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

var app = builder.Build();

app.UseMiddleware<ErrorHandlingMiddleware>();

if (app.Environment.IsDevelopment())
    app.UseSwaggerDocumentation();

app.UseCors("AllowAll");
app.MapControllers();

// Criar tabela e popular dados de teste na subida
using (var scope = app.Services.CreateScope())
{
    var initializer = scope.ServiceProvider.GetRequiredService<DynamoDbTableInitializer>();
    await initializer.EnsureTableExistsAsync();

    if (app.Environment.IsDevelopment())
    {
        var seeder = scope.ServiceProvider.GetRequiredService<DynamoDbDataSeeder>();
        await seeder.SeedAsync();
    }
}

app.Logger.LogInformation("CaseFraudSys API started — Environment: {Env}", app.Environment.EnvironmentName);

app.Run();

public partial class Program { }
