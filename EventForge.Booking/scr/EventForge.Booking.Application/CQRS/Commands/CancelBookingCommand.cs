using EventForge.CQRS;
using EventForge.Shared.Enums;

namespace EventForge.Booking.Application.CQRS.Commands
{
    /// <summary>
    /// Команда на отмену бронирования
    /// </summary>
    /// <param name="BookingId">Идентификатор бронирования</param>
    /// <param name="UserId">Идентификатор пользователя</param>
    /// <param name="Role">Роль пользователя</param>
    public sealed record CancelBookingCommand(Guid BookingId, Guid UserId, RoleType Role) : IRequest<bool>;

}
