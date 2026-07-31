using EventForge.Booking.Application.CQRS.Commands;
using EventForge.Booking.Domain.Exceptions;
using EventForge.CQRS;

namespace EventForge.Booking.Application.CQRS.Validators;

/// <summary>
/// Валидация команды создания бронирования
/// </summary>
public sealed class CreateBookingCommandValidator : IRequestValidator<CreateBookingCommand>
{
    public void Validate(CreateBookingCommand request)
    {
        if (request.EventId == Guid.Empty)
            throw new ValidationCustomException(nameof(CreateBookingCommand), Guid.Empty.ToString(), "Идентификатор события обязателен.");

        if (request.UserId == Guid.Empty)
            throw new ValidationCustomException(nameof(CreateBookingCommand), Guid.Empty.ToString(), "Идентификатор пользователя обязателен.");
    }
}
