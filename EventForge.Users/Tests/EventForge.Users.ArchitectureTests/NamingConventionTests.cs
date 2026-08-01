using System.Reflection;

using FluentAssertions;

using NetArchTest.Rules;

using Xunit;

namespace EventForge.Users.ArchitectureTests;

/// <summary>
/// Проверяет соблюдение соглашений об именовании классов и интерфейсов
/// в микросервисе Users.
/// </summary>
public sealed class NamingConventionTests
{
    private static readonly Assembly ApplicationAssembly = typeof(Application.Interfaces.IAuthService).Assembly;
    private static readonly Assembly InfrastructureAssembly = typeof(Infrastructure.Context.UsersDbContext).Assembly;
    private static readonly Assembly PresentationAssembly = typeof(Presentation.Controllers.AuthController).Assembly;

    private const string ApplicationNamespace = nameof(EventForge.Users.Application);
    private const string InfrastructureNamespace = nameof(EventForge.Users.Infrastructure);
    private const string PresentationNamespace = nameof(EventForge.Users.Presentation);


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

        result.IsSuccessful.Should().BeTrue();
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

        result.IsSuccessful.Should().BeTrue();
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

        result.IsSuccessful.Should().BeTrue();
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

        result.IsSuccessful.Should().BeTrue();
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

        result.IsSuccessful.Should().BeTrue();
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

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "NamingConvention")]
    public void Infrastructure_Mappers_Should_EndWith_Mapper()
    {
        var result = Types.InAssembly(InfrastructureAssembly)
            .That()
            .ResideInNamespace($"{InfrastructureNamespace}.Mapping")
            .And()
            .AreClasses()
            .Should()
            .HaveNameEndingWith("Mapper")
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }
}
