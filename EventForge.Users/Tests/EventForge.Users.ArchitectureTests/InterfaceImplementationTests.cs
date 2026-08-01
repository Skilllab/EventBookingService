using System;
using System.Linq;
using System.Reflection;

using FluentAssertions;

using NetArchTest.Rules;

using Xunit;

namespace EventForge.Users.ArchitectureTests;

/// <summary>
/// Проверяет, что интерфейсы Application-слоя реализованы в Infrastructure,
/// а также что реализации находятся в правильных пространствах имён.
/// </summary>
public sealed class InterfaceImplementationTests
{
    private static readonly Assembly DomainAssembly = typeof(Domain.Entities.User).Assembly;
    private static readonly Assembly InfrastructureAssembly = typeof(Infrastructure.Context.UsersDbContext).Assembly;

    [Fact]
    [Trait("Category", "InterfaceImplementation")]
    public void PasswordHasher_Should_Be_Implemented_In_Infrastructure()
    {
        var result = Types.InAssembly(InfrastructureAssembly)
            .That()
            .ImplementInterface(typeof(Application.Interfaces.IPasswordHasher))
            .And()
            .AreClasses()
            .Should()
            .HaveName("PasswordHasher")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            "IPasswordHasher должен быть реализован как PasswordHasher в Infrastructure.");
    }





    [Fact]
    [Trait("Category", "InterfaceImplementation")]
    public void JwtTokenGenerator_Interface_Should_Have_Implementation_In_Infrastructure()
    {
        var implementations = Types.InAssembly(InfrastructureAssembly)
            .That()
            .ImplementInterface(typeof(Application.Interfaces.IJwtTokenGenerator))
            .And()
            .AreClasses()
            .GetTypes()
            .ToList();

        implementations.Should().NotBeEmpty(
            "IJwtTokenGenerator должен иметь хотя бы одну реализацию в Infrastructure.");
    }

    [Fact]
    [Trait("Category", "InterfaceImplementation")]
    public void PasswordHasher_Interface_Should_Have_Implementation_In_Infrastructure()
    {
        var implementations = Types.InAssembly(InfrastructureAssembly)
            .That()
            .ImplementInterface(typeof(Application.Interfaces.IPasswordHasher))
            .And()
            .AreClasses()
            .GetTypes()
            .ToList();

        implementations.Should().NotBeEmpty(
            "IPasswordHasher должен иметь хотя бы одну реализацию в Infrastructure.");
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
                        && t.Namespace.StartsWith(nameof(EventForge.Users.Domain.Entities), StringComparison.Ordinal)
                        && t.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
                            .Any(c => c.GetParameters().Length == 0))
            .Select(t => t.FullName)
            .ToList();

        violations.Should().BeEmpty(
            $"Доменные сущности не должны иметь public ctor(). Нарушители: {string.Join(", ", violations)}");
    }
}
