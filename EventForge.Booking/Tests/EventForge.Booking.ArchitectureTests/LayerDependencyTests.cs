using System.Reflection;

using FluentAssertions;

using NetArchTest.Rules;

namespace EventForge.Booking.ArchitectureTests;

/// <summary>
/// Проверяет соблюдение правил зависимостей между слоями Clean Architecture
/// для микросервиса Booking.
/// </summary>
public sealed class LayerDependencyTests
{
    // Сборки слоёв
    private static readonly Assembly DomainAssembly = typeof(Domain.Entities.BookingModel).Assembly;
    private static readonly Assembly ApplicationAssembly = typeof(Application.Interfaces.IBookingService).Assembly;
    private static readonly Assembly InfrastructureAssembly = typeof(Infrastructure.Context.BookingDbContext).Assembly;
    private static readonly Assembly PresentationAssembly = typeof(Presentation.Controllers.BookingsController).Assembly;

    // Пространства имён слоёв
    private const string DomainNamespace = nameof(EventForge.Booking.Domain);
    private const string ApplicationNamespace = nameof(EventForge.Booking.Application);
    private const string InfrastructureNamespace = nameof(EventForge.Booking.Infrastructure);
    private const string PresentationNamespace = nameof(EventForge.Booking.Presentation);

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
        var result = Types.InAssembly(PresentationAssembly)
            .That()
            .DoNotHaveName("Program")
            .And()
            .DoNotHaveName("DependencyInjection")
            .And()
            .ResideInNamespace(PresentationNamespace)
            .ShouldNot()
            .HaveDependencyOn(InfrastructureNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            $"Код Presentation не должен ссылаться на типы Infrastructure.\nОшибки:\n{string.Join("\n", result.FailingTypeNames ?? [])}");
    }
}
