using EventForge.Booking.Application.DTO;
using EventForge.CQRS;
using EventForge.Shared.Enums;

namespace EventForge.Booking.Application.CQRS.Queries
{
    /// <summary>
    /// Запрос на получение всех бронирований
    /// </summary>
    /// <param name="UserId">Идентификатор пользователя</param>
    /// <param name="Role">Роль пользователя</param>
    public sealed record GetAllBookingsQuery(Guid UserId, RoleType Role) : IRequest<List<BookingInfoDTO>>;

}
