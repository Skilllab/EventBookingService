using EventForge.Events.Application;
using EventForge.Events.Infrastructure;
using EventForge.Events.Infrastructure.Context;
using EventForge.Events.Presentation;
using EventForge.ExceptionMiddleware;
using EventForge.Swagger;

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
    var db = scope.ServiceProvider.GetRequiredService<EventsDbContext>();
    db.Database.Migrate();
}

if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Docker"))
{
    app.UseSwagger();
    app.UseSharedSwaggerUI("Events");
}

// Глобальный обработчик ошибок
app.UseMiddleware<GlobalExceptionHandlingMiddleware>();

// Включаем CORS (должен быть выше авторизации и контроллеров!)
app.UseCors();

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
