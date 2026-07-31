using EventForge.Events.Application.CQRS.Commands;
using EventForge.Events.Application.CQRS.Validators;
using EventForge.Events.Application.DTO;
using EventForge.Events.Domain.Exceptions;

using FluentAssertions;

using Microsoft.Extensions.Time.Testing;

namespace EventForge.Events.UnitTests;

public class ValidatorsTests
{

    [Fact]
    [Trait("Category", "ChangeEventValidator")]
    public void ChangeEventValidator_Should_Pass_When_All_Fields_Are_Valid()
    {
        // Arrange
        FakeTimeProvider fakeTime = new(new DateTimeOffset(2025, 7, 4, 10, 0, 0, TimeSpan.Zero));
        var validator = new ChangeEventCommandValidator(fakeTime);
        var futureDate = fakeTime.GetUtcNow().UtcDateTime.AddDays(1);
        var updateDto = UpdateEventDto.Create(
            title: "Новое название старого события",
            startAt: futureDate,
            endAt: futureDate.AddHours(2),
            description: "Новое описание старого события");
        var command = new ChangeEventCommand(Guid.NewGuid(), updateDto);

        // Act
        var act = () => validator.Validate(command);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    [Trait("Category", "ChangeEventValidator")]
    public void ChangeEventValidator_Should_Pass_When_Only_Title_Provided()
    {
        // Arrange 
        FakeTimeProvider fakeTime = new(new DateTimeOffset(2025, 7, 4, 10, 0, 0, TimeSpan.Zero));
        var validator = new ChangeEventCommandValidator(fakeTime);
        var updateDto = UpdateEventDto.Create(title: "Только заголовок у суперсобытия");
        var command = new ChangeEventCommand(Guid.NewGuid(), updateDto);

        // Act
        var act = () => validator.Validate(command);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    [Trait("Category", "ChangeEventValidator")]
    public void ChangeEventValidator_Should_Pass_When_Only_Description_Provided()
    {
        // Arrange
        FakeTimeProvider fakeTime = new(new DateTimeOffset(2025, 7, 4, 10, 0, 0, TimeSpan.Zero));
        var validator = new ChangeEventCommandValidator(fakeTime);
        var updateDto = UpdateEventDto.Create(description: "Только описание у суперсобытия");
        var command = new ChangeEventCommand(Guid.NewGuid(), updateDto);

        // Act
        var act = () => validator.Validate(command);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    [Trait("Category", "ChangeEventValidator")]
    public void ChangeEventValidator_Should_Throw_When_Event_Is_Null()
    {
        // Arrange
        FakeTimeProvider fakeTime = new(new DateTimeOffset(2025, 7, 4, 10, 0, 0, TimeSpan.Zero));
        var validator = new ChangeEventCommandValidator(fakeTime);
        var command = new ChangeEventCommand(Guid.NewGuid(), null!);

        // Act
        var act = () => validator.Validate(command);

        // Assert
        act.Should().Throw<ValidationCustomException>();
    }

    [Fact]
    [Trait("Category", "ChangeEventValidator")]
    public void ChangeEventValidator_Should_Throw_When_EventId_Is_Empty()
    {
        // Arrange
        FakeTimeProvider fakeTime = new(new DateTimeOffset(2025, 7, 4, 10, 0, 0, TimeSpan.Zero));
        var validator = new ChangeEventCommandValidator(fakeTime);
        var updateDto = UpdateEventDto.Create(title: "Название у суперсобятия такое");
        var command = new ChangeEventCommand(Guid.Empty, updateDto);

        // Act
        var act = () => validator.Validate(command);

        // Assert
        act.Should().Throw<ValidationCustomException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [Trait("Category", "ChangeEventValidator")]
    public void ChangeEventValidator_Should_Throw_When_Title_Is_Empty_String(string title)
    {
        // Arrange
        FakeTimeProvider fakeTime = new(new DateTimeOffset(2025, 7, 4, 10, 0, 0, TimeSpan.Zero));
        var validator = new ChangeEventCommandValidator(fakeTime);
        var updateDto = UpdateEventDto.Create(title: title);
        var command = new ChangeEventCommand(Guid.NewGuid(), updateDto);

        // Act
        var act = () => validator.Validate(command);

        // Assert
        act.Should().Throw<ValidationCustomException>();
    }
       

    [Fact]
    [Trait("Category", "ChangeEventValidator")]
    public void ChangeEventValidator_Should_Throw_When_StartAt_Is_In_The_Past()
    {
        // Arrange
        FakeTimeProvider fakeTime = new(new DateTimeOffset(2025, 7, 4, 10, 0, 0, TimeSpan.Zero));
        var validator = new ChangeEventCommandValidator(fakeTime);
        var pastDate = fakeTime.GetUtcNow().UtcDateTime.AddDays(-1);
        var updateDto = UpdateEventDto.Create(startAt: pastDate);
        var command = new ChangeEventCommand(Guid.NewGuid(), updateDto);

        // Act
        var act = () => validator.Validate(command);

        // Assert
        act.Should().Throw<ValidationCustomException>();
    }

    [Fact]
    [Trait("Category", "ChangeEventValidator")]
    public void ChangeEventValidator_Should_Throw_When_StartAt_Equals_Now()
    {
        // Arrange
        FakeTimeProvider fakeTime = new(new DateTimeOffset(2025, 7, 4, 10, 0, 0, TimeSpan.Zero));
        var validator = new ChangeEventCommandValidator(fakeTime);
        var now = fakeTime.GetUtcNow().UtcDateTime;
        var updateDto = UpdateEventDto.Create(startAt: now);
        var command = new ChangeEventCommand(Guid.NewGuid(), updateDto);

        // Act
        var act = () => validator.Validate(command);

        // Assert — StartAt <= now → ошибка
        act.Should().Throw<ValidationCustomException>();
    }

    [Fact]
    [Trait("Category", "ChangeEventValidator")]
    public void ChangeEventValidator_Should_Throw_When_EndAt_Is_In_The_Past()
    {
        // Arrange
        FakeTimeProvider fakeTime = new(new DateTimeOffset(2025, 7, 4, 10, 0, 0, TimeSpan.Zero));
        var validator = new ChangeEventCommandValidator(fakeTime);
        var pastEnd = fakeTime.GetUtcNow().UtcDateTime.AddDays(-1);
        var updateDto = UpdateEventDto.Create(endAt: pastEnd);
        var command = new ChangeEventCommand(Guid.NewGuid(), updateDto);

        // Act
        var act = () => validator.Validate(command);

        // Assert
        act.Should().Throw<ValidationCustomException>();
    }

    [Fact]
    [Trait("Category", "ChangeEventValidator")]
    public void ChangeEventValidator_Should_Throw_When_StartAt_Not_Before_EndAt()
    {
        // Arrange
        FakeTimeProvider fakeTime = new(new DateTimeOffset(2025, 7, 4, 10, 0, 0, TimeSpan.Zero));
        var validator = new ChangeEventCommandValidator(fakeTime);
        var futureDate = fakeTime.GetUtcNow().UtcDateTime.AddDays(1);
        var updateDto = UpdateEventDto.Create(startAt: futureDate, endAt: futureDate);
        var command = new ChangeEventCommand(Guid.NewGuid(), updateDto);

        // Act
        var act = () => validator.Validate(command);

        // Assert
        act.Should().Throw<ValidationCustomException>();
    }


    [Fact]
    [Trait("Category", "CreateEventValidator")]
    public void CreateEventValidator_Should_Pass_When_All_Fields_Are_Valid()
    {
        // Arrange
        FakeTimeProvider fakeTime = new(new DateTimeOffset(2025, 7, 4, 10, 0, 0, TimeSpan.Zero));
        var validator = new CreateEventCommandValidator(fakeTime);
        var futureDate = fakeTime.GetUtcNow().UtcDateTime.AddDays(1);
        var dto = new CreateEventDto(
            title: "Новая конференция",
            startAt: futureDate,
            endAt: futureDate.AddHours(3),
            totalSeats: 100,
            description: "Описание");
        var command = new CreateEventCommand(dto);

        // Act
        var act = () => validator.Validate(command);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    [Trait("Category", "CreateEventValidator")]
    public void CreateEventValidator_Should_Pass_When_Description_Is_Null()
    {
        // Arrange
        FakeTimeProvider fakeTime = new(new DateTimeOffset(2025, 7, 4, 10, 0, 0, TimeSpan.Zero));
        var validator = new CreateEventCommandValidator(fakeTime);
        var futureDate = fakeTime.GetUtcNow().UtcDateTime.AddDays(1);
        var dto = new CreateEventDto("Тест", futureDate, futureDate.AddHours(1), 10, null);
        var command = new CreateEventCommand(dto);

        // Act
        var act = () => validator.Validate(command);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    [Trait("Category", "CreateEventValidator")]
    public void CreateEventValidator_Should_Throw_When_Event_Is_Null()
    {
        // Arrange
        FakeTimeProvider fakeTime = new(new DateTimeOffset(2025, 7, 4, 10, 0, 0, TimeSpan.Zero));
        var validator = new CreateEventCommandValidator(fakeTime);
        var command = new CreateEventCommand(null!);

        // Act
        var act = () => validator.Validate(command);

        // Assert
        act.Should().Throw<ValidationCustomException>();
    }

    [Fact]
    [Trait("Category", "CreateEventValidator")]
    public void CreateEventValidator_Should_Throw_When_StartAt_Is_In_The_Past()
    {
        // Arrange
        FakeTimeProvider fakeTime = new(new DateTimeOffset(2025, 7, 4, 10, 0, 0, TimeSpan.Zero));
        var validator = new CreateEventCommandValidator(fakeTime);
        var pastDate = fakeTime.GetUtcNow().UtcDateTime.AddDays(-1);
        var dto = new CreateEventDto("Конференция", pastDate, pastDate.AddHours(2), 10);
        var command = new CreateEventCommand(dto);

        // Act
        var act = () => validator.Validate(command);

        // Assert
        act.Should().Throw<ValidationCustomException>();
    }

    [Fact]
    [Trait("Category", "CreateEventValidator")]
    public void CreateEventValidator_Should_Throw_When_StartAt_Equals_Now()
    {
        // Arrange
        FakeTimeProvider fakeTime = new(new DateTimeOffset(2025, 7, 4, 10, 0, 0, TimeSpan.Zero));
        var validator = new CreateEventCommandValidator(fakeTime);
        var now = fakeTime.GetUtcNow().UtcDateTime;
        var dto = new CreateEventDto("Конференция", now, now.AddHours(1), 10);
        var command = new CreateEventCommand(dto);

        // Act
        var act = () => validator.Validate(command);

        // Assert — StartAt <= now → ошибка
        act.Should().Throw<ValidationCustomException>();
    }
}
