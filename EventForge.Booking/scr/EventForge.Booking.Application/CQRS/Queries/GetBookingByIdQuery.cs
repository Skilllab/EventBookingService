using EventForge.Booking.Application.DTO;
using EventForge.CQRS;

namespace EventForge.Booking.Application.CQRS.Queries
{
    /// <summary>
    /// Запрос на получение бронирования по идентификатору
    /// </summary>
    /// <param name="BookingId">Идентификатор бронирования</param>
    public sealed record GetBookingByIdQuery(Guid BookingId) : IRequest<BookingInfoDTO>;

}
