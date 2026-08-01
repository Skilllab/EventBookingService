using EventForge.Booking.Application.CQRS.Commands;

using FluentValidation;

namespace EventForge.Booking.Application.CQRS.Validators;

/// <summary>
/// Валидация команды отмены бронирования
/// </summary>
public sealed class CancelBookingCommandValidator : AbstractValidator<CancelBookingCommand>
{
    public CancelBookingCommandValidator()
    {
        // Останавливаем проверку при первой ошибке
        RuleLevelCascadeMode = CascadeMode.Stop;

        RuleFor(x => x.BookingId)
            .NotEqual(Guid.Empty)
            .WithMessage("Идентификатор бронирования обязателен");

        RuleFor(x => x.UserId)
            .NotEqual(Guid.Empty)
            .WithMessage("Идентификатор пользователя обязателен");
    }
}
