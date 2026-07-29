namespace EventForge.Contract.Brokers;

/// <summary>
/// Имена Kafka-топиков, общие для издателей и подписчиков
/// </summary>
public static class TopicNames
{
    /// <summary>
    /// Топик подтвержденных бронирований
    /// </summary>
    public const string BookingConfirmed = "booking-confirmed";

    /// <summary>
    /// Топик отклоненных бронирований
    /// </summary>
    public const string BookingRejected = "booking-rejected";

    /// <summary>
    /// Топик отмененных бронирований
    /// </summary>
    public const string BookingCancelled = "booking-cancelled";

    /// <summary>
    /// Топик отклоненных бронирований из-за отсутствия мест
    /// </summary>
    public const string BookingNotApproved = "booking-not-approved";

    /// <summary>
    /// Топик запросов на бронирование
    /// </summary>
    public const string BookingRequested = "booking-requested";

    /// <summary>
    /// Топик повторных запросов на бронирование
    /// </summary>
    public const string BookingRequestedRetry = "booking-requested-retry";

    /// <summary>
    /// Топик сообщений, которые не удалось обработать
    /// </summary>
    public const string BookingRequestedDlq = "booking-requested-dlq";

}
