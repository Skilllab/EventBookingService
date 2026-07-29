using System;
using System.Collections.Generic;
using System.Text;

namespace EventForge.Contract.Brokers
{
    /// <summary>
    /// Envelope для отложенной повторной обработки BookingRequested.
    /// </summary>
    public sealed record BookingRequestedRetryEnvelope(
        BookingRequested Message,
        int RetryAttempt,
        DateTime FirstFailedAtUtc,
        DateTime NextAttemptAtUtc,
        string LastError,
        string RawPayload);
}
