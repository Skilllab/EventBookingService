using System;
using System.IO;
using System.Reflection;

using Asp.Versioning;
using Asp.Versioning.ApiExplorer;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;

namespace EventForge.Swagger;

public static class SwaggerExtensions
{
    public static IServiceCollection AddSharedSwagger(this IServiceCollection services, string apiTitle)
    {
        var apiVersioningBuilder = services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(1, 0); // Версия по умолчанию
            options.AssumeDefaultVersionWhenUnspecified = true; // Использовать дефолтную версию, если она не указана
            options.ReportApiVersions = true; // Отдавать доступные версии в заголовках ответа
            options.ApiVersionReader = new UrlSegmentApiVersionReader(); // Читать версию из URL
        });

        // 2. Интеграция версионирования с ApiExplorer
        apiVersioningBuilder.AddApiExplorer(options =>
        {
            options.GroupNameFormat = "'v'VVV"; // Название группы в формате 'v1', 'v2'
            options.SubstituteApiVersionInUrl = true; // Подставлять версию в роуты эндпоинтов
        });

        services.AddOptions<Swashbuckle.AspNetCore.SwaggerGen.SwaggerGenOptions>()
            .Configure<IServiceProvider>((options, sp) =>
            {

                var provider = sp.GetService<IApiVersionDescriptionProvider>();

                if (provider != null)
                {
                    foreach (var description in provider.ApiVersionDescriptions)
                    {
                        options.SwaggerDoc(description.GroupName, new OpenApiInfo
                        {
                            Title = $"{apiTitle} {description.ApiVersion}",
                            Version = description.GroupName
                        });
                    }
                }
                else
                {
                    options.SwaggerDoc("v1", new OpenApiInfo { Title = apiTitle, Version = "v1" });
                }

                var baseDirectory = AppContext.BaseDirectory;
                var xmlFiles = Directory.GetFiles(baseDirectory, "*.xml");

                foreach (var xmlPath in xmlFiles)
                {
                    // Проверяем, что это XML от нашей экосистемы EventForge, чтобы не читать системные файлы
                    if (Path.GetFileName(xmlPath).StartsWith("EventForge", StringComparison.OrdinalIgnoreCase))
                    {
                        options.IncludeXmlComments(xmlPath);
                    }
                }

                options.EnableAnnotations();

                var securityScheme = new OpenApiSecurityScheme
                {
                    Description = "Введите JWT токен в формате: Bearer {ваш_токен}",
                    Name = "Authorization",
                    Scheme = "Bearer",
                    BearerFormat = "JWT",
                    Type = SecuritySchemeType.Http,
                    In = ParameterLocation.Header,
                };

                options.AddSecurityDefinition("Bearer", securityScheme);

                options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
                {
                    [new OpenApiSecuritySchemeReference("Bearer", document)] = []
                });
            });

        services.AddSwaggerGen();

        return services;
    }


    private static readonly FileExtensionContentTypeProvider ContentTypeProvider = new();
    private static readonly Assembly CurrentAssembly = typeof(SwaggerExtensions).Assembly;

    // Выносим префикс пространства имен сборки для Embedded Resources
    private const string ResourcePrefix = "EventForge.Swagger.wwwroot";

    public static IApplicationBuilder UseSharedSwaggerUI(this IApplicationBuilder app, string serviceName)
    {
        //Упрощенное Middleware для раздачи статики из Embedded Resources
        app.Use(async (context, next) =>
        {
            var path = context.Request.Path.Value ?? "";

            // Превращаем URL-путь в формат имени Embedded Resource (заменяем '/' на '.')
            // Пример: "/js/swagger-copy-token.js" -> "EventForge.Swagger.wwwroot.js.swagger-copy-token.js"
            var resourceName = $"{ResourcePrefix}{path.Replace('/', '.')}";

            // Проверяем, существует ли такой манифест в сборке. Ресурсы то добавили как embedded
            if (CurrentAssembly.GetManifestResourceInfo(resourceName) != null)
            {
                await using var stream = CurrentAssembly.GetManifestResourceStream(resourceName);
                if (stream != null)
                {
                    // Автоматически определяем Content-Type по расширению файла
                    if (!ContentTypeProvider.TryGetContentType(path, out var contentType))
                    {
                        contentType = "application/octet-stream";
                    }

                    context.Response.ContentType = contentType;
                    context.Response.Headers.CacheControl = "public, max-age=3600";

                    await stream.CopyToAsync(context.Response.Body);
                    return;
                }
            }
            await next();
        });

        // Настройка SwaggerUI
        app.UseSwaggerUI(options =>
        {
            var provider = app.ApplicationServices.GetRequiredService<IApiVersionDescriptionProvider>();

            foreach (var description in provider.ApiVersionDescriptions)
            {
                options.SwaggerEndpoint(
                    $"/swagger/{description.GroupName}/swagger.json",
                    $"EventForge {serviceName} {description.GroupName.ToUpperInvariant()}"
                );
            }

            options.DisplayRequestDuration();
            options.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.List);
            options.DocumentTitle = $"Сервис {serviceName}";

            // Инжекция JS
            options.InjectJavascript("/js/swagger-change-head.js");
            options.InjectJavascript("/js/swagger-change-select-api.js");
            options.InjectJavascript("/js/swagger-copy-token.js");
            options.InjectJavascript("/js/swagger-set-icons.js");

            // Инжекция CSS
            options.InjectStylesheet("/theme-material.css");
            options.InjectStylesheet("/swagger-customization.css");
        });

        return app;
    }


}
