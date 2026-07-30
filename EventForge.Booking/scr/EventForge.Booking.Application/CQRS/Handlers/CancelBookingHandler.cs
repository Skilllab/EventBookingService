using EventForge.Booking.Application.CQRS.Commands;
using EventForge.Booking.Application.Interfaces;
using EventForge.CQRS;

namespace EventForge.Booking.Application.CQRS.Handlers
{
    /// <summary>
    /// Обработчик команды отмены бронирования
    /// </summary>
    /// <param name="service">Сервис для работы с бронированиями</param>
    public sealed class CancelBookingHandler(IBookingService service) : IRequestHandler<CancelBookingCommand, bool>
    {
        public Task<bool> Handle(CancelBookingCommand request, CancellationToken ct) =>
            service.CancelBooking(request.BookingId, request.UserId, request.Role, ct);
    }
}
