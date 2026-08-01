using System;

using EventForge.Events.Application.CQRS.Commands;

using FluentValidation;

namespace EventForge.Events.Application.CQRS.Validators;

/// <summary>
/// Валидация команды создания события
/// </summary>
public class CreateEventCommandValidator : AbstractValidator<CreateEventCommand>
{
    public CreateEventCommandValidator(TimeProvider timeProvider)
    {
        RuleFor(x => x.Event)
            .NotNull()
            .WithMessage("Событие обязательно");

        RuleFor(x => x.Event.Title)
            .NotEmpty()
            .When(x => x.Event != null)
            .WithMessage("Название события обязательно");

        RuleFor(x => x.Event.StartAt)
            .GreaterThan(x => timeProvider.GetUtcNow().UtcDateTime)
            .When(x => x.Event != null)
            .WithMessage("Дата начала события должна быть больше текущего момента");

        RuleFor(x => x.Event.StartAt)
            .LessThan(x => x.Event.EndAt)
            .When(x => x.Event != null && x.Event.EndAt != default)
            .WithMessage("Дата начала события должна быть раньше даты окончания");
    }
}
