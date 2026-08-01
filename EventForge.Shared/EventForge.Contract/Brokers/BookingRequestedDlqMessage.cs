using System;

namespace EventForge.Contract.Brokers
{
    /// <summary>
    /// Сообщение для DLQ по BookingRequested.
    /// </summary>
    public sealed record BookingRequestedDlqMessage(
        Guid DlqMessageId,
        string SourceTopic,
        string RawPayload,
        string Error,
        DateTime FailedAtUtc,
        int RetryAttempt,
        Guid? OriginalMessageId);
}
