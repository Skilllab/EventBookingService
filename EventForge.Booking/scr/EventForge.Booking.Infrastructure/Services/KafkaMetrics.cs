using System;
using System.Diagnostics.Metrics;

namespace EventForge.Booking.Infrastructure.Services;

/// <summary>
/// RED-метрики (Rate/Errors/Duration) для Kafka-операций и бизнес-счётчики бронирований.
/// </summary>
public class KafkaMetrics : IDisposable
{
    private readonly Meter _meter;

    // ── RED: Kafka Producer ──
    public Counter<long> PublishedMessages { get; }
    public Counter<long> PublishErrors { get; }
    public Histogram<double> PublishDuration { get; }

    // ── RED: Kafka Consumer ──
    public Counter<long> ConsumedMessages { get; }
    public Counter<long> ConsumerErrors { get; }
    public Histogram<double> ConsumerDuration { get; }

    // ── Бизнес-метрики ──
    public Counter<long> BookingsCreated { get; }
    public Counter<long> BookingsConfirmed { get; }
    public Counter<long> BookingsRejected { get; }
    public Counter<long> BookingsCancelled { get; }

    public KafkaMetrics(IMeterFactory meterFactory)
    {
        _meter = meterFactory.Create("EventForge.Booking.Kafka");

        PublishedMessages = _meter.CreateCounter<long>("kafka_published_total",
            description: "Количество опубликованных в Kafka сообщений");
        PublishErrors = _meter.CreateCounter<long>("kafka_publish_errors_total",
            description: "Количество ошибок публикации в Kafka");
        PublishDuration = _meter.CreateHistogram<double>("kafka_publish_duration_seconds",
            description: "Длительность публикации в Kafka, секунды",
            unit: "s");

        ConsumedMessages = _meter.CreateCounter<long>("kafka_consumed_total",
            description: "Количество потреблённых из Kafka сообщений");
        ConsumerErrors = _meter.CreateCounter<long>("kafka_consumer_errors_total",
            description: "Количество ошибок потребления из Kafka");
        ConsumerDuration = _meter.CreateHistogram<double>("kafka_consumer_duration_seconds",
            description: "Длительность обработки потреблённого сообщения, секунды",
            unit: "s");

        BookingsCreated = _meter.CreateCounter<long>("bookings_created_total",
            description: "Всего создано бронирований");
        BookingsConfirmed = _meter.CreateCounter<long>("bookings_confirmed_total",
            description: "Всего подтверждено бронирований");
        BookingsRejected = _meter.CreateCounter<long>("bookings_rejected_total",
            description: "Всего отклонено бронирований");
        BookingsCancelled = _meter.CreateCounter<long>("bookings_cancelled_total",
            description: "Всего отменено бронирований пользователями");
    }

    public void Dispose()
    {
        _meter.Dispose();
    }
}
