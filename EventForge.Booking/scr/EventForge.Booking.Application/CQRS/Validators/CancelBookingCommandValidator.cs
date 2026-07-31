using EventForge.Booking.Application.CQRS.Commands;
using EventForge.Booking.Domain.Exceptions;
using EventForge.CQRS;

namespace EventForge.Booking.Application.CQRS.Validators
{
    /// <summary>
    /// Валидация команды отмены бронирования
    /// </summary>
    internal class CancelBookingCommandValidator :  IRequestValidator<CancelBookingCommand>
    {
        public void Validate(CancelBookingCommand request)
        {
            if (request.BookingId == Guid.Empty)
                throw new ValidationCustomException(nameof(CancelBookingCommand), Guid.Empty.ToString(), "Идентификатор бронирования обязателен");

            if (request.UserId == Guid.Empty)
                throw new ValidationCustomException(nameof(CancelBookingCommand), Guid.Empty.ToString(), "Идентификатор пользователя обязателен");
        }
    }
}
