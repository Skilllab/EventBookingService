using System.Reflection;

using FluentAssertions;

using NetArchTest.Rules;

namespace EventForge.Booking.ArchitectureTests;

/// <summary>
/// Проверяет, что интерфейсы Application-слоя реализованы в Infrastructure,
/// а также что реализации находятся в правильных пространствах имён.
/// </summary>
public sealed class InterfaceImplementationTests
{
    private static readonly Assembly DomainAssembly = typeof(Domain.Entities.BookingModel).Assembly;
    private static readonly Assembly ApplicationAssembly = typeof(Application.Interfaces.IBookingService).Assembly;
    private static readonly Assembly InfrastructureAssembly = typeof(Infrastructure.Context.BookingDbContext).Assembly;

    [Fact]
    [Trait("Category", "InterfaceImplementation")]
    public void Repositories_Should_Implement_Application_Interfaces()
    {
        var result = Types.InAssembly(InfrastructureAssembly)
            .That()
            .ResideInNamespace(nameof(EventForge.Booking.Infrastructure.Repositories))
            .And()
            .AreClasses()
            .Should()
            .ImplementInterface(typeof(Application.Interfaces.IBookingRepository))
            .Or()
            .ImplementInterface(typeof(Application.Interfaces.IOutboxRepository))
            .Or()
            .ImplementInterface(typeof(Application.Interfaces.IProcessedMessageRepository))
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            $"Все репозитории должны реализовывать хотя бы один Application-интерфейс.\nОшибки:\n{string.Join("\n", result.FailingTypeNames ?? [])}");
    }

    [Fact]
    [Trait("Category", "InterfaceImplementation")]
    public void Cqrs_Handlers_Should_Implement_IRequestHandler()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .That()
            .ResideInNamespace(nameof(EventForge.Booking.Application.CQRS.Handlers))
            .And()
            .AreClasses()
            .Should()
            .ImplementInterface(typeof(CQRS.IRequestHandler<,>))
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            $"Все обработчики CQRS должны реализовывать IRequestHandler<,>.\nОшибки:\n{string.Join("\n", result.FailingTypeNames ?? [])}");
    }

    [Fact]
    [Trait("Category", "InterfaceImplementation")]
    public void BookingPublisher_Interface_Should_Have_Implementation_In_Infrastructure()
    {
        var implementations = Types.InAssembly(InfrastructureAssembly)
            .That()
            .ImplementInterface(typeof(Application.Interfaces.IBookingPublisher))
            .And()
            .AreClasses()
            .GetTypes()
            .ToList();

        implementations.Should().NotBeEmpty(
            "IBookingPublisher должен иметь хотя бы одну реализацию в Infrastructure.");
    }

    [Fact]
    [Trait("Category", "InterfaceImplementation")]
    public void Domain_Entities_Should_Not_Have_Public_Parameterless_Constructor()
    {
        var violations = DomainAssembly
            .GetTypes()
            .Where(t => t.IsClass
                        && !t.IsAbstract
                        && t.Namespace is not null
                        && t.Namespace.StartsWith(nameof(EventForge.Booking.Domain.Entities), StringComparison.Ordinal)
                        && t.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
                            .Any(c => c.GetParameters().Length == 0))
            .Select(t => t.FullName)
            .ToList();

        violations.Should().BeEmpty(
            $"Доменные сущности не должны иметь public ctor(). Нарушители: {string.Join(", ", violations)}");
    }
}
