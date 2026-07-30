using EventForge.Booking.Application.DTO;
using EventForge.CQRS;

namespace EventForge.Booking.Application.CQRS.Commands;

/// <summary>
/// Команда на создание бронирования
/// </summary>
/// <param name="EventId">Идентификатор события</param>
/// <param name="UserId">Идентификатор пользователя</param>
public sealed record CreateBookingCommand(Guid EventId, Guid UserId) : IRequest<BookingInfoDTO>;
