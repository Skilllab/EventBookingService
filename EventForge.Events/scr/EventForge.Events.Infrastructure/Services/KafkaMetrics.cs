using System;
using System.Diagnostics.Metrics;

namespace EventForge.Events.Infrastructure.Services;

/// <summary>
/// RED-метрики (Rate/Errors/Duration) для Kafka-операций и бизнес-счётчики событий/мест.
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
    public Counter<long> BookingRequestsReceived { get; }
    public Counter<long> BookingRequestsConfirmed { get; }
    public Counter<long> BookingRequestsRejected { get; }
    public Counter<long> BookingRequestsNotApproved { get; }
    public Counter<long> SeatsReserved { get; }
    public Counter<long> SeatsReleased { get; }
    public Counter<long> EventsCreated { get; }
    public Counter<long> EventsCancelled { get; }

    public KafkaMetrics(IMeterFactory meterFactory)
    {
        _meter = meterFactory.Create("EventForge.Events.Kafka");

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

        BookingRequestsReceived = _meter.CreateCounter<long>("booking_requests_received_total",
            description: "Всего получено запросов на бронирование");
        BookingRequestsConfirmed = _meter.CreateCounter<long>("booking_requests_confirmed_total",
            description: "Всего подтверждено запросов на бронирование");
        BookingRequestsRejected = _meter.CreateCounter<long>("booking_requests_rejected_total",
            description: "Всего отклонено запросов (событие не найдено)");
        BookingRequestsNotApproved = _meter.CreateCounter<long>("booking_requests_not_approved_total",
            description: "Всего не одобрено запросов (нет мест / событие началось)");
        SeatsReserved = _meter.CreateCounter<long>("seats_reserved_total",
            description: "Всего зарезервировано мест");
        SeatsReleased = _meter.CreateCounter<long>("seats_released_total",
            description: "Всего освобождено мест");
        EventsCreated = _meter.CreateCounter<long>("events_created_total",
            description: "Всего создано событий");
        EventsCancelled = _meter.CreateCounter<long>("events_cancelled_total",
            description: "Всего отменено событий");
    }

    public void Dispose()
    {
        _meter.Dispose();
    }
}
