using EventForge.ExceptionMiddleware;
using EventForge.Swagger;
using EventForge.Users.Application;
using EventForge.Users.Infrastructure;
using EventForge.Users.Infrastructure.Context;
using EventForge.Users.Presentation;

using Microsoft.EntityFrameworkCore;

using Serilog;
using Serilog.Formatting.Compact;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddConsole();

builder.Host.UseSerilog((ctx, cfg) =>
    cfg.ReadFrom.Configuration(ctx.Configuration)
        .WriteTo.Console(new CompactJsonFormatter()));

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication(builder.Configuration);
builder.Services.AddPresentation(builder.Configuration);

if (builder.Environment.IsDevelopment())
{
    builder.Host.UseDefaultServiceProvider(options =>
    {
        options.ValidateScopes = true;
        options.ValidateOnBuild = true;
    });
}


var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<UsersDbContext>();
    db.Database.Migrate();
}

if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Docker"))
{
    app.UseSwagger();
    app.UseSharedSwaggerUI("Users");
}

// Глобальный обработчик ошибок
app.UseMiddleware<GlobalExceptionHandlingMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.MapPrometheusScrapingEndpoint(); // доступен по /metrics

app.MapControllers();

app.Run();

/// <summary>
/// Частичный класс Program, необходимый для интеграционного тестирования
/// </summary>
public partial class Program { }
