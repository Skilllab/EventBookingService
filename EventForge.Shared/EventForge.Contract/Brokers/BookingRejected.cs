using System;

namespace EventForge.Contract.Brokers;

/// <summary>
/// Контракт события отклоненного бронирования
/// </summary>
/// <param name="MessageId">Идентификатор сообщения</param>
/// <param name="BookingId">Идентификатор бронирования</param>
/// <param name="EventId">Идентификатор события (мероприятия)</param>
/// <param name="UserId">Идентификатор пользователя</param>
/// <param name="RejectedAt">Дата и время отклонения бронирования</param>
/// <param name="Reason">Причина отклонения</param>
public sealed record BookingRejected(
    Guid MessageId,
    Guid BookingId,
    Guid EventId,
    Guid UserId,
    DateTime RejectedAt, string Reason);
