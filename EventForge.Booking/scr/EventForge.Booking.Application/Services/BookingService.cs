using System.Text.Json;

using EventForge.Booking.Application.Common;
using EventForge.Booking.Application.DTO;
using EventForge.Booking.Application.Interfaces;
using EventForge.Booking.Domain.Entities;
using EventForge.Booking.Domain.Exceptions;
using EventForge.Contract.Brokers;
using EventForge.Shared.Enums;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EventForge.Booking.Application.Services;

/// <summary>
/// Сервис для работы с бронированием
/// </summary>
public class BookingService(
    IBookingRepository bookingRepository,
    IOptions<BookingOptions> bookingOptions,
    ILogger<BookingService> logger,
    TimeProvider timeProvider) : IBookingService
{
    private readonly BookingOptions _bookingOptions = bookingOptions.Value;

    public async Task<BookingInfoDTO> CreateBookingAsync(Guid eventId, Guid userId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        logger.LogInformation("Создание нового бронирования для события: {Event}", eventId);

        var activeBookingsCount = await bookingRepository.GetUserActiveBookingsCountAsync(userId, ct);
        if (activeBookingsCount >= _bookingOptions.MaxBookingCount)
        {
            throw new BookingLimitExceededException(
                nameof(BookingModel),
                userId.ToString(),
                $"Превышено количество допустимых бронирований. Допустимо: {_bookingOptions.MaxBookingCount}");
        }

        var newBooking = BookingModel.Create(
            eventId,
            userId,
            timeProvider.GetUtcNow().UtcDateTime);


        var createdMessage = new BookingRequested(
            Guid.NewGuid(),
            newBooking.Id,
            newBooking.EventId,
            newBooking.UserId,
            1,
            newBooking.CreatedAt);

        var createdOutbox = OutboxMessage.Create(
            type: nameof(BookingRequested),
            topic: TopicNames.BookingRequested,
            messageKey: eventId.ToString(),
            payload: JsonSerializer.Serialize(createdMessage),
            createdAt: newBooking.CreatedAt,
            error: null);

        await bookingRepository.CreateAndAddOutboxAsync(newBooking, createdOutbox, ct);
        logger.LogInformation("Бронирование успешно создано. ID: {Id}", newBooking.Id);
        return MapToDTO(newBooking);
    }

    public async Task<BookingInfoDTO> GetBookingByIdAsync(Guid bookingId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        logger.LogInformation("Получение бронирования: {bookingId}", bookingId);

        var booking = await bookingRepository.GetByIdAsync(bookingId, ct);
        return booking == null
            ? throw new NotFoundException(nameof(BookingModel), bookingId.ToString())
            : MapToDTO(booking);
    }

    public async Task<bool> CancelBooking(Guid bookingId, Guid userId, RoleType userRole, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var userBooking = await bookingRepository.GetByIdAsync(bookingId, ct);
        if (userBooking == null)
            throw new NotFoundException(nameof(BookingModel), bookingId.ToString());

        if (userBooking.UserId != userId && userRole != RoleType.Admin)
        {
            throw new InsufficientPermissionsException(
                nameof(BookingModel),
                bookingId.ToString(),
                "У пользователя недостаточно прав для отмены бронирования");
        }

        if (userBooking.Status == BookingStatus.Cancelled)
            throw new InvalidOperationException($"Бронирование '{bookingId}' уже было отменено");

        if (userBooking.Status == BookingStatus.Rejected)
            throw new InvalidOperationException($"Невозможно отменить уже отклонённое бронирование '{bookingId}'");

        var cancelledAt = timeProvider.GetUtcNow().UtcDateTime;

        var cancelledMessage = new BookingCancelled(
            Guid.NewGuid(),
            userBooking.Id,
            userBooking.EventId,
            userBooking.UserId,
            1,
            cancelledAt);

        var cancelledOutbox = OutboxMessage.Create(
            type: nameof(BookingCancelled),
            topic: TopicNames.BookingCancelled,
            messageKey: userBooking.EventId.ToString(),
            payload: JsonSerializer.Serialize(cancelledMessage),
            createdAt: cancelledAt,
            error: null);

        var saved = await bookingRepository.CancelAndAddOutboxAsync(
            userBooking.Id,
            userBooking.UserId,
            cancelledAt,
            cancelledOutbox,
            ct);

        if (!saved)
            throw new InvalidOperationException($"Не удалось отменить бронирование '{bookingId}'");

        logger.LogInformation("Бронирование успешно отменено. ID: {Id}", bookingId);
        return true;
    }

    public async Task<List<BookingInfoDTO>> GetAllBooking(Guid userId, RoleType roleType, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        logger.LogInformation("Получение всех бронирований для пользователя: {userId} с ролью: {roleType}", userId, roleType);


        if (roleType != RoleType.Admin)
        {
            throw new InsufficientPermissionsException(
                nameof(BookingModel),
                "У пользователя недостаточно прав для просмотра списка бронирвоаний");
        }

        var bookings = await bookingRepository.GetAllAsync(ct);
        bookings = bookings.Where(b => b.UserId == userId).ToList();
        return bookings.Select(MapToDTO).ToList();
    }

    private static BookingInfoDTO MapToDTO(BookingModel booking) =>
        new(
            ID: booking.Id,
            EventID: booking.EventId,
            Status: booking.Status.ToString());
}
