using EventForge.Booking.Application.CQRS.Commands;
using EventForge.Booking.Application.DTO;
using EventForge.Booking.Application.Interfaces;
using EventForge.CQRS;

namespace EventForge.Booking.Application.CQRS.Handlers
{
    /// <summary>
    /// Обработчик команды создания бронирования
    /// </summary>
    /// <param name="service">Сервис для работы с бронированиями</param>
    public sealed class CreateBookingHandler(IBookingService service) : IRequestHandler<CreateBookingCommand, BookingInfoDTO>
    {
        public Task<BookingInfoDTO> Handle(CreateBookingCommand request, CancellationToken ct) =>
            service.CreateBookingAsync(request.EventId, request.UserId, ct);
    }
}
