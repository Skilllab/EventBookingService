using EventForge.Booking.Application.CQRS.Commands;

using FluentValidation;

namespace EventForge.Booking.Application.CQRS.Validators;

/// <summary>
/// Валидация команды создания бронирования
/// </summary>
public sealed class CreateBookingCommandValidator : AbstractValidator<CreateBookingCommand>
{
    public CreateBookingCommandValidator()
    {
        // Останавливаем проверку при первой ошибке
        RuleLevelCascadeMode = CascadeMode.Stop;

        RuleFor(x => x.EventId)
            .NotEqual(Guid.Empty)
            .WithMessage("Идентификатор события обязателен.");

        RuleFor(x => x.UserId)
            .NotEqual(Guid.Empty)
            .WithMessage("Идентификатор пользователя обязателен.");
    }
}
