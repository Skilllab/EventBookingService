using System.ComponentModel.DataAnnotations;

using EventForge.Events.Presentation.ValidationAttributes;

namespace EventForge.Events.Presentation.DTO;

/// <summary>
/// Запрос создания события
/// </summary>
public class CreateEventRequest
{
    /// <summary>
    /// Название события
    /// </summary>
    [Required(ErrorMessage = "Наименование для события обязательно для заполнения.")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Дата начала события
    /// </summary>
    [Required(ErrorMessage = "Дата начала события не может быть пустой")]
    [NotMinDateTime(ErrorMessage = "Дата начала события должна быть задана")]
    public DateTime StartAt { get; set; }

    /// <summary>
    /// Дата окончания события
    /// </summary>
    [Required(ErrorMessage = "Дата окончания события не может быть пустой")]
    [NotMinDateTime(ErrorMessage = "Дата окончания события должна быть задана")]
    [DateGreater(nameof(StartAt))]
    public DateTime EndAt { get; set; }

    /// <summary>
    /// Общее количество мест на событии
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "Общее количество мест для события должно быть больше нуля")]
    public int TotalSeats { get; set; }

    /// <summary>
    /// Описание события
    /// </summary>
    public string? Description { get; set; } = "";
}