using System.ComponentModel.DataAnnotations;

using EventForge.Events.Presentation.ValidationAttributes;

namespace EventForge.Events.Presentation.DTO;

/// <summary>
/// Фильтр получения событий
/// </summary>
public class EventsFilterRequest
{
    /// <summary>
    /// Название события
    /// </summary>
    [StringLength(100, ErrorMessage = "Название для поиска слишком длинное")]
    public string? Title { get; set; }

    /// <summary>
    /// Дата начала события
    /// </summary>
    public DateTime? From { get; set; }

    /// <summary>
    /// Дата завершения события
    /// </summary>
    [DateGreater("From")]
    public DateTime? To { get; set; }

    /// <summary>
    /// Страница, которую необходимо вернуть
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "Номер страницы должен быть больше 0")]
    public int Page { get; set; } = 1;

    /// <summary>
    /// Количество элементов на странице
    /// </summary>
    [Range(1, 100, ErrorMessage = "Размер страницы должен быть от 1 до 100 элементов")]
    public int PageSize { get; set; } = 10;
}