using EventForge.Booking.Application.CQRS.Queries;
using EventForge.Booking.Application.DTO;
using EventForge.Booking.Application.Interfaces;
using EventForge.CQRS;

namespace EventForge.Booking.Application.CQRS.Handlers
{
    /// <summary>
    /// Обработчик запроса получения всех бронирований
    /// </summary>
    /// <param name="service">Сервис для работы с бронированиями</param>
    public sealed class GetAllBookingsHandler(IBookingService service) : IRequestHandler<GetAllBookingsQuery, List<BookingInfoDTO>>
    {
        public Task<List<BookingInfoDTO>> Handle(GetAllBookingsQuery request, CancellationToken ct) =>
            service.GetAllBooking(request.UserId, request.Role, ct);
    }
}
