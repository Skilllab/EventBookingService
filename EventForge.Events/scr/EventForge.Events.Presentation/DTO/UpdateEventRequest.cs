using EventForge.Events.Presentation.ValidationAttributes;

namespace EventForge.Events.Presentation.DTO;

/// <summary>
/// DTO для изменения события
/// </summary>
public class UpdateEventRequest
{
    /// <summary>
    /// Название события
    /// </summary>
    public string? Title { get; set; } = null;

    /// <summary>
    /// Описание события
    /// </summary>
    public string? Description { get; set; } = null;

    /// <summary>
    /// Дата начала события
    /// </summary>
    public DateTime? StartAt { get; set; } = null;

    /// <summary>
    /// Дата завершения события
    /// </summary>
    [DateGreater(nameof(StartAt))]
    public DateTime? EndAt { get; set; } = null;
}