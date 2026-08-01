using System.Reflection;

using FluentAssertions;

using NetArchTest.Rules;

using Xunit;

namespace EventForge.Events.ArchitectureTests;

/// <summary>
/// Проверяет соблюдение правил зависимостей между слоями Clean Architecture.
/// Domain → ни от кого не зависит.
/// Application → зависит только от Domain.
/// Infrastructure → зависит от Application и Domain.
/// Presentation → зависит от Application (проектная ссылка на Infrastructure допустима
/// только для DI-регистрации, но код не должен использовать типы Infrastructure напрямую).
/// </summary>
public sealed class LayerDependencyTests
{

    private static readonly Assembly DomainAssembly = typeof(EventForge.Events.Domain.Entities.Event).Assembly;
    private static readonly Assembly ApplicationAssembly = typeof(EventForge.Events.Application.Interfaces.IEventService).Assembly;
    private static readonly Assembly InfrastructureAssembly = typeof(EventForge.Events.Infrastructure.Context.EventsDbContext).Assembly;
    private static readonly Assembly PresentationAssembly = typeof(EventForge.Events.Presentation.Controllers.EventsController).Assembly;

    private const string DomainNamespace = nameof(EventForge.Events.Domain);
    private const string ApplicationNamespace = nameof(EventForge.Events.Application);
    private const string InfrastructureNamespace = nameof(EventForge.Events.Infrastructure);
    private const string PresentationNamespace = nameof(EventForge.Events.Presentation);


    [Fact]
    [Trait("Category", "LayerDependency")]
    public void Domain_Should_Not_Depend_On_Application_Infrastructure_Or_Presentation()
    {
        var result = Types.InAssembly(DomainAssembly)
            .That()
            .ResideInNamespace(DomainNamespace)
            .ShouldNot()
            .HaveDependencyOnAny(
                ApplicationNamespace,
                InfrastructureNamespace,
                PresentationNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            $"Domain не должен зависеть от Application/Infrastructure/Presentation.\nОшибки:\n{string.Join("\n", result.FailingTypeNames ?? [])}");
    }
  

    [Fact]
    [Trait("Category", "LayerDependency")]
    public void Application_Should_Not_Depend_On_Infrastructure_Or_Presentation()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .That()
            .ResideInNamespace(ApplicationNamespace)
            .ShouldNot()
            .HaveDependencyOnAny(
                InfrastructureNamespace,
                PresentationNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            $"Application не должен зависеть от Infrastructure/Presentation.\nОшибки:\n{string.Join("\n", result.FailingTypeNames ?? [])}");
    }


    [Fact]
    [Trait("Category", "LayerDependency")]
    public void Infrastructure_Should_Not_Depend_On_Presentation()
    {
        var result = Types.InAssembly(InfrastructureAssembly)
            .That()
            .ResideInNamespace(InfrastructureNamespace)
            .ShouldNot()
            .HaveDependencyOn(PresentationNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            $"Infrastructure не должен зависеть от Presentation.\nОшибки:\n{string.Join("\n", result.FailingTypeNames ?? [])}");
    }


    [Fact]
    [Trait("Category", "LayerDependency")]
    public void Presentation_Code_Should_Not_Use_Infrastructure_Types_Directly()
    {
        // Исключаем Program.cs, так как он отвечает за DI-композицию
        var result = Types.InAssembly(PresentationAssembly)
            .That()
            .DoNotHaveName("Program")          // Program.cs — разрешён для DI
            .And()
            .DoNotHaveName("DependencyInjection") // DI-класс — разрешён
            .And()
            .ResideInNamespace(PresentationNamespace)
            .ShouldNot()
            .HaveDependencyOn(InfrastructureNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            $"Код Presentation не должен ссылаться на типы Infrastructure.\nОшибки:\n{string.Join("\n", result.FailingTypeNames ?? [])}");
    }

  
}
