using EventForge.Users.Application.CQRS.Commands;
using EventForge.Users.Application.CQRS.Queries;
using EventForge.Users.Application.CQRS.Validators;
using EventForge.Users.Domain.Exceptions;

using FluentAssertions;

namespace EventForge.Users.UnitTests;

public class ValidatorsTests
{
    [Fact]
    [Trait("Category", "LoginUserValidator")]
    public void LoginUserValidator_Should_Pass_When_Both_Fields_Are_Valid()
    {
        // Arrange
        var validator = new LoginUserQueryValidator();
        var query = new LoginUserQuery("validLogin", "validPass");

        // Act
        var act = () => validator.Validate(query);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    [Trait("Category", "LoginUserValidator")]
    public void LoginUserValidator_Should_Throw_When_Login_Is_Null()
    {
        // Arrange
        var validator = new LoginUserQueryValidator();
        var query = new LoginUserQuery(null!, "password");

        // Act
        var act = () => validator.Validate(query);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(null, "password")]
    [InlineData("", "password")]
    [InlineData("   ", "password")]
    [Trait("Category", "LoginUserValidator")]
    public void LoginUserValidator_Should_Throw_When_Login_Is_Invalid(string login, string password)
    {
        // Arrange
        var validator = new LoginUserQueryValidator();
        var query = new LoginUserQuery(login, password);

        // Act
        var act = () => validator.Validate(query);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("login", null)]
    [InlineData("login", "")]
    [InlineData("login", "   ")]
    [Trait("Category", "LoginUserValidator")]
    public void LoginUserValidator_Should_Throw_When_Password_Is_Invalid(string login, string password)
    {
        // Arrange
        var validator = new LoginUserQueryValidator();
        var query = new LoginUserQuery(login, password);

        // Act
        var act = () => validator.Validate(query);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    [Trait("Category", "LoginUserValidator")]
    public void LoginUserValidator_Should_Throw_When_Both_Fields_Are_Empty()
    {
        // Arrange
        var validator = new LoginUserQueryValidator();
        var query = new LoginUserQuery("", "");

        // Act
        var act = () => validator.Validate(query);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    [Trait("Category", "RegisterUserValidator")]
    public void RegisterUserValidator_Should_Pass_When_All_Fields_Are_Valid()
    {
        // Arrange
        var validator = new RegisterUserCommandValidator();
        var command = new RegisterUserCommand("validLogin", "validPass", "User");

        // Act
        var act = () => validator.Validate(command);

        // Assert
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData("login", "password", null)]
    [InlineData("login", "password", "")]
    [InlineData("login", "password", "   ")]
    [Trait("Category", "RegisterUserValidator")]
    public void RegisterUserValidator_Should_Pass_When_Role_Is_Not_Set(string login, string password, string role)
    {
        // Arrange
        var validator = new RegisterUserCommandValidator();
        var command = new RegisterUserCommand(login, password, role);

        // Act
        var act = () => validator.Validate(command);

        // Assert
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData("login", "password", "Admin")]
    [InlineData("login", "password", "aDmIn ")]
    [Trait("Category", "RegisterUserValidator")]
    public void RegisterUserValidator_Should_Pass_When_Role_Is_Admin(string login, string password, string role)
    {
        // Arrange
        var validator = new RegisterUserCommandValidator();
        var command = new RegisterUserCommand(login, password, role);

        // Act
        var act = () => validator.Validate(command);

        // Assert
        act.Should().NotThrow();
    }

  

    [Fact]
    [Trait("Category", "RegisterUserValidator")]
    public void RegisterUserValidator_Should_Pass_When_Login_Exactly_3_Chars()
    {
        // Arrange
        var validator = new RegisterUserCommandValidator();
        var command = new RegisterUserCommand("abc", "validPass", null);

        // Act
        var act = () => validator.Validate(command);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    [Trait("Category", "RegisterUserValidator")]
    public void RegisterUserValidator_Should_Pass_When_Login_Exactly_64_Chars()
    {
        // Arrange
        var validator = new RegisterUserCommandValidator();
        var login = new string('x', 64);
        var command = new RegisterUserCommand(login, "validPass", null);

        // Act
        var act = () => validator.Validate(command);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    [Trait("Category", "RegisterUserValidator")]
    public void RegisterUserValidator_Should_Pass_When_Password_Exactly_6_Chars()
    {
        // Arrange
        var validator = new RegisterUserCommandValidator();
        var command = new RegisterUserCommand("validLogin", "123456", null);

        // Act
        var act = () => validator.Validate(command);

        // Assert
        act.Should().NotThrow();
    }


    [Fact]
    [Trait("Category", "RegisterUserValidator")]
    public void RegisterUserValidator_Should_Throw_When_Login_Is_Too_Short()
    {
        // Arrange
        var validator = new RegisterUserCommandValidator();
        var command = new RegisterUserCommand("ab", "validPass", null);

        // Act
        var act = () => validator.Validate(command);

        // Assert
        act.Should().Throw<ValidationCustomException>();
    }

    [Fact]
    [Trait("Category", "RegisterUserValidator")]
    public void RegisterUserValidator_Should_Throw_When_Login_Is_Too_Long()
    {
        // Arrange
        var validator = new RegisterUserCommandValidator();
        var login = new string('x', 65);
        var command = new RegisterUserCommand(login, "validPass", null);

        // Act
        var act = () => validator.Validate(command);

        // Assert
        act.Should().Throw<ValidationCustomException>();
    }

    [Fact]
    [Trait("Category", "RegisterUserValidator")]
    public void RegisterUserValidator_Should_Throw_When_Password_Is_Too_Short()
    {
        // Arrange
        var validator = new RegisterUserCommandValidator();
        var command = new RegisterUserCommand("validLogin", "12345", null);

        // Act
        var act = () => validator.Validate(command);

        // Assert
        act.Should().Throw<ValidationCustomException>();
    }

    [Fact]
    [Trait("Category", "RegisterUserValidator")]
    public void RegisterUserValidator_Should_Throw_When_Role_Is_Invalid()
    {
        // Arrange
        var validator = new RegisterUserCommandValidator();
        var command = new RegisterUserCommand("validLogin", "validPass", "SuperUser");

        // Act
        var act = () => validator.Validate(command);

        // Assert
        act.Should().Throw<ValidationCustomException>();
    }
}
