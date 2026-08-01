using System;

using EventForge.Events.Application.CQRS.Commands;

using FluentValidation;

namespace EventForge.Events.Application.CQRS.Validators;

public sealed class ChangeEventCommandValidator : AbstractValidator<ChangeEventCommand>
{
    public ChangeEventCommandValidator(TimeProvider timeProvider)
    {
        // Прекращает всю валидацию класса, как только ЛЮБОЕ правило вернет ошибку
        ClassLevelCascadeMode = CascadeMode.Stop;

        RuleFor(x => x.Event)
            .NotNull()
            .WithMessage("Данные для обновления события обязательны");

        RuleFor(x => x.EventId)
            .NotEqual(Guid.Empty)
            .WithMessage("Идентификатор события обязателен");

        // Группируем проверки вложенного объекта Event
        When(x => x.Event != null, () =>
        {
            RuleFor(x => x.Event.Title)
                // Оптимизация: стандартный метод NotEmpty делает то же самое, что и !string.IsNullOrWhiteSpace
                .NotEmpty()
                .When(x => x.Event.Title != null)
                .WithMessage("Название события не может быть пустым");

            RuleFor(x => x.Event.StartAt)
                .GreaterThan(x => timeProvider.GetUtcNow().UtcDateTime)
                .When(x => x.Event.StartAt != null)
                .WithMessage("Дата начала события должна быть больше текущего момента");

            RuleFor(x => x.Event.EndAt)
                .GreaterThan(x => timeProvider.GetUtcNow().UtcDateTime)
                .When(x => x.Event.EndAt != null)
                .WithMessage("Дата окончания события должна быть больше текущего момента");

            // Для кросс-свойств лучше использовать Must на конкретное свойство, передавая команду вторым параметром
            RuleFor(x => x.Event.StartAt)
                .LessThan(x => x.Event.EndAt)
                .When(x => x.Event.StartAt != null && x.Event.EndAt != null)
                .WithMessage("Дата начала события должна быть раньше даты окончания");
        });
    }
}
