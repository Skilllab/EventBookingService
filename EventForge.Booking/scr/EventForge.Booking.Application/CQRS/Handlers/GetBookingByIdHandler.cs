using EventForge.Booking.Application.CQRS.Queries;
using EventForge.Booking.Application.DTO;
using EventForge.Booking.Application.Interfaces;
using EventForge.CQRS;

namespace EventForge.Booking.Application.CQRS.Handlers
{
    /// <summary>
    /// Обработчик запроса получения бронирования по идентификатору
    /// </summary>
    /// <param name="service">Сервис для работы с бронированиями</param>
    public sealed class GetBookingByIdHandler(IBookingService service) : IRequestHandler<GetBookingByIdQuery, BookingInfoDTO>
    {
        public Task<BookingInfoDTO> Handle(GetBookingByIdQuery request, CancellationToken ct) =>
            service.GetBookingByIdAsync(request.BookingId, ct);
    }
}
