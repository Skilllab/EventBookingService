using System;

namespace EventForge.Contract.Brokers;

/// <summary>
/// Контракт события создаваемого бронирования
/// </summary>
/// <param name="MessageId">Идентификатор сообщения</param>
/// <param name="BookingId">Идентификатор бронирования</param>
/// <param name="EventId">Идентификатор события (мероприятия)</param>
/// <param name="UserId">Идентификатор пользователя</param>
/// <param name="SeatsCount">Количество мест для резервирования</param>
/// <param name="CreatedAt">Дата и время создания бронирования</param>
public sealed record BookingRequested(
    Guid MessageId,
    Guid BookingId,
    Guid EventId,
    Guid UserId,
    int SeatsCount,
    DateTime CreatedAt);
