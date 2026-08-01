using System.Reflection;

using FluentAssertions;

using NetArchTest.Rules;

using Xunit;

namespace EventForge.Events.ArchitectureTests;

/// <summary>
/// Проверяет соблюдение соглашений об именовании классов и интерфейсов.
/// </summary>
public sealed class NamingConventionTests
{
    private static readonly Assembly ApplicationAssembly = typeof(EventForge.Events.Application.Interfaces.IEventService).Assembly;
    private static readonly Assembly InfrastructureAssembly = typeof(EventForge.Events.Infrastructure.Context.EventsDbContext).Assembly;
    private static readonly Assembly PresentationAssembly = typeof(EventForge.Events.Presentation.Controllers.EventsController).Assembly;

    private const string ApplicationNamespace = nameof(EventForge.Events.Application);
    private const string InfrastructureNamespace = nameof(EventForge.Events.Infrastructure);
    private const string PresentationNamespace = nameof(EventForge.Events.Presentation);

    [Fact]
    [Trait("Category", "NamingConvention")]
    public void Controllers_Should_EndWith_Controller()
    {
        var result = Types.InAssembly(PresentationAssembly)
            .That()
            .ResideInNamespace($"{PresentationNamespace}.Controllers")
            .And()
            .AreClasses()
            .Should()
            .HaveNameEndingWith("Controller")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            $"Контроллеры должны заканчиваться на 'Controller'.\nОшибки:\n{string.Join("\n", result.FailingTypeNames ?? [])}");
    }

    [Fact]
    [Trait("Category", "NamingConvention")]
    public void Cqrs_Commands_Should_EndWith_Command()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .That()
            .ResideInNamespace($"{ApplicationNamespace}.CQRS.Commands")
            .And()
            .AreClasses()
            .Should()
            .HaveNameEndingWith("Command")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            $"Команды должны заканчиваться на 'Command'.\nОшибки:\n{string.Join("\n", result.FailingTypeNames ?? [])}");
    }


    [Fact]
    [Trait("Category", "NamingConvention")]
    public void Cqrs_Queries_Should_EndWith_Query()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .That()
            .ResideInNamespace($"{ApplicationNamespace}.CQRS.Queries")
            .And()
            .AreClasses()
            .Should()
            .HaveNameEndingWith("Query")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            $"Запросы должны заканчиваться на 'Query'.\nОшибки:\n{string.Join("\n", result.FailingTypeNames ?? [])}");
    }


    [Fact]
    [Trait("Category", "NamingConvention")]
    public void Cqrs_Handlers_Should_EndWith_Handler()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .That()
            .ResideInNamespace($"{ApplicationNamespace}.CQRS.Handlers")
            .And()
            .AreClasses()
            .Should()
            .HaveNameEndingWith("Handler")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            $"Обработчики должны заканчиваться на 'Handler'.\nОшибки:\n{string.Join("\n", result.FailingTypeNames ?? [])}");
    }

    [Fact]
    [Trait("Category", "NamingConvention")]
    public void Interfaces_Should_StartWith_I()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .That()
            .AreInterfaces()
            .Should()
            .HaveNameStartingWith("I")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            $"Интерфейсы должны начинаться с 'I'.\nОшибки:\n{string.Join("\n", result.FailingTypeNames ?? [])}");
    }


    [Fact]
    [Trait("Category", "NamingConvention")]
    public void Application_Services_Should_EndWith_Service()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .That()
            .ResideInNamespace($"{ApplicationNamespace}.Services")
            .And()
            .AreClasses()
            .Should()
            .HaveNameEndingWith("Service")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            $"Сервисы Application должны заканчиваться на 'Service'.\nОшибки:\n{string.Join("\n", result.FailingTypeNames ?? [])}");
    }


    [Fact]
    [Trait("Category", "NamingConvention")]
    public void Application_DTOs_Should_EndWith_Dto()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .That()
            .ResideInNamespace($"{ApplicationNamespace}.DTO")
            .And()
            .AreClasses()
            .Should()
            .HaveNameEndingWith("Dto")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            $"DTO должны заканчиваться на 'Dto'.\nОшибки:\n{string.Join("\n", result.FailingTypeNames ?? [])}");
    }


    [Fact]
    [Trait("Category", "NamingConvention")]
    public void Repositories_Should_EndWith_Repository()
    {
        var result = Types.InAssembly(InfrastructureAssembly)
            .That()
            .ResideInNamespace($"{InfrastructureNamespace}.Repositories")
            .And()
            .AreClasses()
            .Should()
            .HaveNameEndingWith("Repository")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            $"Репозитории должны заканчиваться на 'Repository'.\nОшибки:\n{string.Join("\n", result.FailingTypeNames ?? [])}");
    }


    [Fact]
    [Trait("Category", "NamingConvention")]
    public void Infrastructure_Services_Should_Have_Conventional_Suffix()
    {
        var result = Types.InAssembly(InfrastructureAssembly)
            .That()
            .ResideInNamespace($"{InfrastructureNamespace}.Services")
            .And()
            .AreClasses()
            .And()
            .ImplementInterface(typeof(Microsoft.Extensions.Hosting.IHostedService))
            .Should()
            .HaveNameEndingWith("Consumer")
            .Or()
            .HaveNameEndingWith("BackgroundService")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            $"Фоновые сервисы должны заканчиваться на 'Consumer' или 'BackgroundService'.\nОшибки:\n{string.Join("\n", result.FailingTypeNames ?? [])}");
    }


    [Fact]
    [Trait("Category", "NamingConvention")]
    public void Mappers_Should_EndWith_Mapper()
    {
        var appResult = Types.InAssembly(ApplicationAssembly)
            .That()
            .ResideInNamespace($"{ApplicationNamespace}.Mapping")
            .And()
            .AreClasses()
            .Should()
            .HaveNameEndingWith("Mapper")
            .GetResult();

        var infraResult = Types.InAssembly(InfrastructureAssembly)
            .That()
            .ResideInNamespace($"{InfrastructureNamespace}.Mappers")
            .And()
            .AreClasses()
            .Should()
            .HaveNameEndingWith("Mapper")
            .GetResult();

        var presResult = Types.InAssembly(PresentationAssembly)
            .That()
            .ResideInNamespace($"{PresentationNamespace}.Mapping")
            .And()
            .AreClasses()
            .Should()
            .HaveNameEndingWith("Mapper")
            .GetResult();

        appResult.IsSuccessful.Should().BeTrue("Мапперы Application должны заканчиваться на 'Mapper'");
        infraResult.IsSuccessful.Should().BeTrue("Мапперы Infrastructure должны заканчиваться на 'Mapper'");
        presResult.IsSuccessful.Should().BeTrue("Мапперы Presentation должны заканчиваться на 'Mapper'");
    }
}
