using System;

using EventForge.Contract.Enums;

namespace EventForge.Contract.Brokers;

/// <summary>
/// Контракт события отклоненного бронирования
/// </summary>
/// <param name="MessageId">Идентификатор сообщения</param>
/// <param name="BookingId">Идентификатор бронирования</param>
/// <param name="EventId">Идентификатор события (мероприятия)</param>
/// <param name="UserId">Идентификатор пользователя</param>
/// <param name="RejectedAt">Дата и время отклонения бронирования</param>
/// <param name="Reason">Причина отклонения бронирования</param>
public sealed record BookingNotApproved(
    Guid MessageId,
    Guid BookingId,
    Guid EventId,
    Guid UserId,
    DateTime RejectedAt, BookingNotApprovedReason Reason);
