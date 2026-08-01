using System;

namespace EventForge.CacheKeys;

public static class KeysForEvents
{
    // Статический ключ для ТОП-10
    public const string TopEvents = "events:top10";

    // Динамический ключ для одиночного события
    public static string ForEvent(Guid id) => $"event:{id}";

}
