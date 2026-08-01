using System;
using System.Threading.Tasks;

using EventForge.Shared.Enums;
using EventForge.Users.Application.Interfaces;
using EventForge.Users.Application.Services;
using EventForge.Users.Domain.Entities;

using FluentAssertions;

using Moq;

using Xunit;

namespace EventForge.Users.UnitTests;

public class AuthServiceTests
{

    [Fact]
    [Trait("Category", "Register")]
    public async Task RegisterUserAsync_Should_Throw_When_Login_Is_Empty()
    {
        // Arrange
        var userRepositoryMock = new Mock<IUserRepository>();
        var passwordHasherMock = new Mock<IPasswordHasher>();
        var tokenGeneratorMock = new Mock<IJwtTokenGenerator>();

        userRepositoryMock
            .Setup(x => x.ExistsAsync(""))
            .ReturnsAsync(false);

        passwordHasherMock
            .Setup(x => x.HashPassword("password"))
            .Returns("hashed-password");

        var service = new AuthService(userRepositoryMock.Object, passwordHasherMock.Object, tokenGeneratorMock.Object);

        // Act
        Func<Task> act = () => service.RegisterUserAsync("", "password", null);

        // Assert
        await act.Should().ThrowAsync<Domain.Exceptions.ValidationCustomException>();
        userRepositoryMock.Verify(x => x.AddAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    [Trait("Category", "Register")]
    public async Task RegisterUserAsync_Should_Throw_When_Password_Is_Empty()
    {
        // Arrange
        var userRepositoryMock = new Mock<IUserRepository>();
        var passwordHasherMock = new Mock<IPasswordHasher>();
        var tokenGeneratorMock = new Mock<IJwtTokenGenerator>();

        userRepositoryMock
            .Setup(x => x.ExistsAsync("new-user"))
            .ReturnsAsync(false);

        passwordHasherMock
            .Setup(x => x.HashPassword(""))
            .Throws< ArgumentException>();

        var service = new AuthService(userRepositoryMock.Object, passwordHasherMock.Object, tokenGeneratorMock.Object);

        // Act
        Func<Task> act = () => service.RegisterUserAsync("new-user", "", null);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
        userRepositoryMock.Verify(x => x.AddAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    [Trait("Category", "Register")]
    public async Task RegisterUserAsync_Should_Use_ParsedRole_CaseInsensitive()
    {
        // Arrange
        var userRepositoryMock = new Mock<IUserRepository>();
        var passwordHasherMock = new Mock<IPasswordHasher>();
        var tokenGeneratorMock = new Mock<IJwtTokenGenerator>();

        userRepositoryMock
            .Setup(x => x.ExistsAsync("case-user"))
            .ReturnsAsync(false);

        passwordHasherMock
            .Setup(x => x.HashPassword("password"))
            .Returns("hashed-password");

        User? savedUser = null;
        userRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<User>()))
            .Callback<User>(user => savedUser = user)
            .Returns(Task.CompletedTask);

        var service = new AuthService(userRepositoryMock.Object, passwordHasherMock.Object, tokenGeneratorMock.Object);

        // Act
        var result = await service.RegisterUserAsync("case-user", "password", "aDmIn");

        // Assert
        result.Should().BeTrue();
        savedUser.Should().NotBeNull();
        savedUser!.Role.Should().Be(RoleType.Admin);
    }



    [Fact]
    [Trait("Category", "Register")]
    public async Task RegisterUserAsync_Should_ReturnFalse_When_User_Already_Exists()
    {
        // Arrange
        var userRepositoryMock = new Mock<IUserRepository>();
        var passwordHasherMock = new Mock<IPasswordHasher>();
        var tokenGeneratorMock = new Mock<IJwtTokenGenerator>();

        userRepositoryMock
            .Setup(x => x.ExistsAsync("existing-user"))
            .ReturnsAsync(true);

        var service = new AuthService(userRepositoryMock.Object, passwordHasherMock.Object, tokenGeneratorMock.Object);
        // Act
        var result = await service.RegisterUserAsync("existing-user", "password", null);
        
        // Assert
        result.Should().BeFalse();
        passwordHasherMock.Verify(x => x.HashPassword(It.IsAny<string>()), Times.Never);
        userRepositoryMock.Verify(x => x.AddAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    [Trait("Category", "Register")]
    public async Task RegisterUserAsync_Should_Save_User_With_Default_Role_When_Role_Is_Not_Provided()
    {
        // Arrange
        var userRepositoryMock = new Mock<IUserRepository>();
        var passwordHasherMock = new Mock<IPasswordHasher>();
        var tokenGeneratorMock = new Mock<IJwtTokenGenerator>();

        userRepositoryMock
            .Setup(x => x.ExistsAsync("new-user"))
            .ReturnsAsync(false);

        passwordHasherMock
            .Setup(x => x.HashPassword("password"))
            .Returns("hashed-password");

        User? savedUser = null;
        userRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<User>()))
            .Callback<User>(user => savedUser = user)
            .Returns(Task.CompletedTask);

        var service = new AuthService(userRepositoryMock.Object, passwordHasherMock.Object, tokenGeneratorMock.Object);
        
        // Act
        var result = await service.RegisterUserAsync("new-user", "password", null);

        // Assert
        result.Should().BeTrue();
        savedUser.Should().NotBeNull();
        savedUser!.Login.Should().Be("new-user");
        savedUser.PasswordHash.Should().Be("hashed-password");
        savedUser.Role.Should().Be(RoleType.User);
    }

    [Fact]
    [Trait("Category", "Register")]
    public async Task RegisterUserAsync_Should_Save_User_With_Parsed_Role_When_Role_Is_Valid()
    {
        // Arrange
        var userRepositoryMock = new Mock<IUserRepository>();
        var passwordHasherMock = new Mock<IPasswordHasher>();
        var tokenGeneratorMock = new Mock<IJwtTokenGenerator>();

        userRepositoryMock
            .Setup(x => x.ExistsAsync("admin-user"))
            .ReturnsAsync(false);

        passwordHasherMock
            .Setup(x => x.HashPassword("password"))
            .Returns("hashed-password");

        User? savedUser = null;
        userRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<User>()))
            .Callback<User>(user => savedUser = user)
            .Returns(Task.CompletedTask);

        var service = new AuthService(userRepositoryMock.Object, passwordHasherMock.Object, tokenGeneratorMock.Object);

        // Act
        var result = await service.RegisterUserAsync("admin-user", "password", "Admin");

        // Assert
        result.Should().BeTrue();
        savedUser.Should().NotBeNull();
        savedUser!.Role.Should().Be(RoleType.Admin);
    }

    [Fact]
    [Trait("Category", "Register")]
    public async Task RegisterUserAsync_Should_Fallback_To_Default_Role_When_Role_Is_Invalid()
    {
        // Arrange
        var userRepositoryMock = new Mock<IUserRepository>();
        var passwordHasherMock = new Mock<IPasswordHasher>();
        var tokenGeneratorMock = new Mock<IJwtTokenGenerator>();

        userRepositoryMock
            .Setup(x => x.ExistsAsync("editor-user"))
            .ReturnsAsync(false);

        passwordHasherMock
            .Setup(x => x.HashPassword("password"))
            .Returns("hashed-password");

        User? savedUser = null;
        userRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<User>()))
            .Callback<User>(user => savedUser = user)
            .Returns(Task.CompletedTask);

        var service = new AuthService(userRepositoryMock.Object, passwordHasherMock.Object, tokenGeneratorMock.Object);

        // Act
        var result = await service.RegisterUserAsync("editor-user", "password", "SuperAdmin");

        // Assert
        result.Should().BeTrue();
        savedUser.Should().NotBeNull();
        savedUser!.Role.Should().Be(RoleType.User);
    }

    [Fact]
    [Trait("Category", "Login")]
    public async Task LoginUserAsync_Should_ReturnNull_When_User_Does_Not_Exist()
    {
        // Arrange
        var userRepositoryMock = new Mock<IUserRepository>();
        var passwordHasherMock = new Mock<IPasswordHasher>();
        var tokenGeneratorMock = new Mock<IJwtTokenGenerator>();

        userRepositoryMock
            .Setup(x => x.GetByLoginAsync("unknown-user"))
            .ReturnsAsync((User?)null);

        var service = new AuthService(userRepositoryMock.Object, passwordHasherMock.Object, tokenGeneratorMock.Object);

        // Act
        var result = await service.LoginUserAsync("unknown-user", "password");

        // Assert
        result.Should().BeNull();
        passwordHasherMock.Verify(x => x.VerifyPassword(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        tokenGeneratorMock.Verify(x => x.GenerateToken(It.IsAny<Guid>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    [Trait("Category", "Login")]
    public async Task LoginUserAsync_Should_ReturnFalse_When_Password_Is_Empty()
    {
        // Arrange
        var userRepositoryMock = new Mock<IUserRepository>();
        var passwordHasherMock = new Mock<IPasswordHasher>();
        var tokenGeneratorMock = new Mock<IJwtTokenGenerator>();

        userRepositoryMock
            .Setup(x => x.GetByLoginAsync("unknown-user"))
            .ReturnsAsync((User?) null);

        var service = new AuthService(userRepositoryMock.Object, passwordHasherMock.Object, tokenGeneratorMock.Object);

        // Act
        var result = await service.LoginUserAsync("unknown-user", "");

        // Assert
        result.Should().BeNull();
        passwordHasherMock.Verify(x => x.VerifyPassword(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        tokenGeneratorMock.Verify(x => x.GenerateToken(It.IsAny<Guid>(), It.IsAny<string>()), Times.Never);
    }



    [Fact]
    [Trait("Category", "Login")]
    public async Task LoginUserAsync_Should_ReturnFalse_When_Login_Is_Empty()
    {
        // Arrange
        var userRepositoryMock = new Mock<IUserRepository>();
        var passwordHasherMock = new Mock<IPasswordHasher>();
        var tokenGeneratorMock = new Mock<IJwtTokenGenerator>();

        userRepositoryMock
            .Setup(x => x.GetByLoginAsync(""))
            .ReturnsAsync((User?) null);

        var service = new AuthService(userRepositoryMock.Object, passwordHasherMock.Object, tokenGeneratorMock.Object);

        // Act
        var result = await service.LoginUserAsync("", "password");

        // Assert
        result.Should().BeNull();
        passwordHasherMock.Verify(x => x.VerifyPassword(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        tokenGeneratorMock.Verify(x => x.GenerateToken(It.IsAny<Guid>(), It.IsAny<string>()), Times.Never);
    }



    [Fact]
    [Trait("Category", "Login")]
    public async Task LoginUserAsync_Should_ReturnNull_When_Password_Is_Invalid()
    {
        // Arrange
        var userRepositoryMock = new Mock<IUserRepository>();
        var passwordHasherMock = new Mock<IPasswordHasher>();
        var tokenGeneratorMock = new Mock<IJwtTokenGenerator>();
        var user = User.Create("known-user", "stored-hash", RoleType.User);

        userRepositoryMock
            .Setup(x => x.GetByLoginAsync("known-user"))
            .ReturnsAsync(user);

        passwordHasherMock
            .Setup(x => x.VerifyPassword("password", "stored-hash"))
            .Returns(false);

        var service = new AuthService(userRepositoryMock.Object, passwordHasherMock.Object, tokenGeneratorMock.Object);

        // Act
        var result = await service.LoginUserAsync("known-user", "password");

        // Assert
        result.Should().BeNull();
        tokenGeneratorMock.Verify(x => x.GenerateToken(It.IsAny<Guid>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    [Trait("Category", "Login")]
    public async Task LoginUserAsync_Should_Return_Token_When_Credentials_Are_Valid()
    {
        // Arrange
        var userRepositoryMock = new Mock<IUserRepository>();
        var passwordHasherMock = new Mock<IPasswordHasher>();
        var tokenGeneratorMock = new Mock<IJwtTokenGenerator>();
        var user = User.Create("known-user", "stored-hash", RoleType.Admin);

        userRepositoryMock
            .Setup(x => x.GetByLoginAsync("known-user"))
            .ReturnsAsync(user);

        passwordHasherMock
            .Setup(x => x.VerifyPassword("password", "stored-hash"))
            .Returns(true);

        tokenGeneratorMock
            .Setup(x => x.GenerateToken(user.Id, "Admin"))
            .Returns("jwt-token");

        var service = new AuthService(userRepositoryMock.Object, passwordHasherMock.Object, tokenGeneratorMock.Object);

        // Act
        var result = await service.LoginUserAsync("known-user", "password");

        // Assert
        result.Should().Be("jwt-token");
        tokenGeneratorMock.Verify(x => x.GenerateToken(user.Id, "Admin"), Times.Once);
    }

}
