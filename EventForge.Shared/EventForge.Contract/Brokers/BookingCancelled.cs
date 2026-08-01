using System;

namespace EventForge.Contract.Brokers;

/// <summary>
/// Контракт события отмененного бронирования
/// </summary>
/// <param name="MessageId">Идентификатор сообщения</param>
/// <param name="BookingId">Идентификатор бронирования</param>
/// <param name="EventId">Идентификатор события (мероприятия)</param>
/// <param name="UserId">Идентификатор пользователя</param>
/// <param name="SeatsCount">Количество отмененных мест</param>
/// <param name="CancelledAt">Дата и время отмены бронирования</param>
public sealed record BookingCancelled(
    Guid MessageId,
    Guid BookingId,
    Guid EventId,
    Guid UserId,
    int SeatsCount,
    DateTime CancelledAt);
