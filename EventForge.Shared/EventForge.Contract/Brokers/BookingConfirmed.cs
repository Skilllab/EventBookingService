using System;

namespace EventForge.Contract.Brokers;

/// <summary>
/// Контракт события подтвержденного бронирования
/// </summary>
/// <param name="MessageId">Уникальный идентификатор сообщения для идемпотентной обработки</param>
/// <param name="BookingId">Идентификатор бронирования</param>
/// <param name="EventId">Идентификатор события</param>
/// <param name="UserId">Идентификатор пользователя</param>
/// <param name="SeatsCount">Количество подтвержденных мест</param>
/// <param name="ConfirmedAt">Время подтверждения</param>
public sealed record BookingConfirmed(
    Guid MessageId,
    Guid BookingId,
    Guid EventId,
    Guid UserId,
    int SeatsCount,
    DateTime ConfirmedAt
);
