using EventForge.CQRS;
using EventForge.Events.Application.CQRS.Commands;
using EventForge.Events.Domain.Exceptions;

namespace EventForge.Events.Application.CQRS.Validators;

/// <summary>
/// Валидация команды создания события
/// </summary>
public sealed class CreateEventCommandValidator(TimeProvider timeProvider) : IRequestValidator<CreateEventCommand>
{
    public void Validate(CreateEventCommand request)
    {
        if (request.Event == null)
            throw new ValidationCustomException(nameof(CreateEventCommand), Guid.Empty.ToString(), "Событие обязательно");

        if (string.IsNullOrWhiteSpace(request.Event.Title))
            throw new ValidationCustomException(nameof(CreateEventCommand), Guid.Empty.ToString(), "Название события обязательно");

        // Проверка: дата начала должна быть в будущем
        if (request.Event.StartAt <= timeProvider.GetUtcNow().UtcDateTime)
            throw new ValidationCustomException(nameof(CreateEventCommand), Guid.Empty.ToString(), "Дата начала события должна быть больше текущего момента");

        // Проверка: дата начала не может быть позже даты окончания
        if (request.Event.StartAt >= request.Event.EndAt)
            throw new ValidationCustomException(nameof(CreateEventCommand), Guid.Empty.ToString(), "Дата начала события должна быть раньше даты окончания");

    }
}
