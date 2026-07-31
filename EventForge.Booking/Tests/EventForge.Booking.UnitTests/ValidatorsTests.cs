using EventForge.Booking.Application.CQRS.Commands;
using EventForge.Booking.Application.CQRS.Validators;
using EventForge.Booking.Domain.Exceptions;
using EventForge.Shared.Enums;

using FluentAssertions;

namespace EventForge.Booking.UnitTests;

public class ValidatorsTests
{
    [Fact]
    [Trait("Category", "CancelBookingValidator")]
    public void CancelBookingValidator_Should_Pass_When_All_Fields_Are_Valid()
    {
        // Arrange
        var validator = new CancelBookingCommandValidator();
        var command = new CancelBookingCommand(
            BookingId: Guid.NewGuid(),
            UserId: Guid.NewGuid(),
            Role: RoleType.User);

        // Act
        var act = () => validator.Validate(command);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    [Trait("Category", "CancelBookingValidator")]
    public void CancelBookingValidator_Should_Throw_When_BookingId_Is_Empty()
    {
        // Arrange
        var validator = new CancelBookingCommandValidator();
        var command = new CancelBookingCommand(
            BookingId: Guid.Empty,
            UserId: Guid.NewGuid(),
            Role: RoleType.User);

        // Act
        var act = () => validator.Validate(command);

        // Assert
        act.Should().Throw<ValidationCustomException>();
    }

    [Fact]
    [Trait("Category", "CancelBookingValidator")]
    public void CancelBookingValidator_Should_Throw_When_UserId_Is_Empty()
    {
        // Arrange
        var validator = new CancelBookingCommandValidator();
        var command = new CancelBookingCommand(
            BookingId: Guid.NewGuid(),
            UserId: Guid.Empty,
            Role: RoleType.Admin);

        // Act
        var act = () => validator.Validate(command);

        // Assert
        act.Should().Throw<ValidationCustomException>();
    }

    [Fact]
    [Trait("Category", "CancelBookingValidator")]
    public void CancelBookingValidator_Should_Throw_When_Both_Ids_Are_Empty()
    {
        // Arrange
        var validator = new CancelBookingCommandValidator();
        var command = new CancelBookingCommand(
            BookingId: Guid.Empty,
            UserId: Guid.Empty,
            Role: RoleType.User);

        // Act
        var act = () => validator.Validate(command);

        // Assert
        act.Should().Throw<ValidationCustomException>();
    }

    [Fact]
    [Trait("Category", "CreateBookingValidator")]
    public void CreateBookingValidator_Should_Pass_When_All_Fields_Are_Valid()
    {
        // Arrange
        var validator = new CreateBookingCommandValidator();
        var command = new CreateBookingCommand(
            EventId: Guid.NewGuid(),
            UserId: Guid.NewGuid());

        // Act
        var act = () => validator.Validate(command);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    [Trait("Category", "CreateBookingValidator")]
    public void CreateBookingValidator_Should_Throw_When_EventId_Is_Empty()
    {
        // Arrange
        var validator = new CreateBookingCommandValidator();
        var command = new CreateBookingCommand(
            EventId: Guid.Empty,
            UserId: Guid.NewGuid());

        // Act
        var act = () => validator.Validate(command);

        // Assert
        act.Should().Throw<ValidationCustomException>();
    }

    [Fact]
    [Trait("Category", "CreateBookingValidator")]
    public void CreateBookingValidator_Should_Throw_When_UserId_Is_Empty()
    {
        // Arrange
        var validator = new CreateBookingCommandValidator();
        var command = new CreateBookingCommand(
            EventId: Guid.NewGuid(),
            UserId: Guid.Empty);

        // Act
        var act = () => validator.Validate(command);

        // Assert
        act.Should().Throw<ValidationCustomException>();
    }

    [Fact]
    [Trait("Category", "CreateBookingValidator")]
    public void CreateBookingValidator_Should_Throw_When_Both_Ids_Are_Empty()
    {
        // Arrange
        var validator = new CreateBookingCommandValidator();
        var command = new CreateBookingCommand(
            EventId: Guid.Empty,
            UserId: Guid.Empty);

        // Act
        var act = () => validator.Validate(command);

        // Assert
        act.Should().Throw<ValidationCustomException>();
    }
}
