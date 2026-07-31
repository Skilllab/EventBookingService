using EventForge.CQRS;
using EventForge.Events.Application.CQRS.Commands;
using EventForge.Events.Domain.Exceptions;

namespace EventForge.Events.Application.CQRS.Validators
{
    internal class ChangeEventCommandValidator(TimeProvider timeProvider) : IRequestValidator<ChangeEventCommand>
    {
        public void Validate(ChangeEventCommand request)
        {
            if (request.Event == null) throw new ValidationCustomException(nameof(ChangeEventCommand), Guid.Empty.ToString(), "Данные для обновления события обязательны");

            if (request.EventId == Guid.Empty)
                throw new ValidationCustomException(nameof(ChangeEventCommand), Guid.Empty.ToString(), "Идентификатор события обязателен");

            if (request.Event.Title is not null && string.IsNullOrWhiteSpace(request.Event.Title))
                throw new ValidationCustomException(nameof(ChangeEventCommand), request.EventId.ToString(), "Название события не может быть пустым");

            if (request.Event is { StartAt : not null }&& request.Event.StartAt.Value <= timeProvider.GetUtcNow().UtcDateTime)
                throw new ValidationCustomException(nameof(ChangeEventCommand), request.EventId.ToString(), "Дата начала события должна быть больше текущего момента");

            if (request.Event is { EndAt : not null }  && request.Event.EndAt.Value <= timeProvider.GetUtcNow().UtcDateTime)
                throw new ValidationCustomException(nameof(ChangeEventCommand), request.EventId.ToString(), "Дата окончания события должна быть больше текущего момента");

            if (request.Event is { StartAt: not null, EndAt: not null } && request.Event.StartAt.Value >= request.Event.EndAt.Value)
                throw new ValidationCustomException(nameof(ChangeEventCommand), request.EventId.ToString(), "Дата начала события должна быть раньше даты окончания");
        }
    }
}
