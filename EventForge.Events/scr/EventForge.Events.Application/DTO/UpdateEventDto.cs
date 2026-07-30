namespace EventForge.Events.Application.DTO;

/// <summary>
/// DTO для изменения события
/// </summary>
public class UpdateEventDto
{
    /// <summary>
    /// Название события
    /// </summary>
    public string? Title { get; private set; }

    /// <summary>
    /// Описание события
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// Дата начала события
    /// </summary>
    public DateTime? StartAt { get; private set; }

    /// <summary>
    /// Дата завершения события
    /// </summary>
    public DateTime? EndAt { get; private set; }

    /// <summary>
    /// Приватный конструктор для защиты от прямого создания через new
    /// </summary>
    private UpdateEventDto(string? title, string? description, DateTime? startAt, DateTime? endAt)
    {
        Title = title;
        Description = description;
        StartAt = startAt;
        EndAt = endAt;
    }

    /// <summary>
    /// Фабричный метод для создания DTO изменения события
    /// </summary>
    public static UpdateEventDto Create(string? title = null, DateTime? startAt = null, DateTime? endAt = null, string? description = null)
    {
        // Обычная проверка через if-else
        if (startAt.HasValue && endAt.HasValue)
        {
            if (endAt.Value < startAt.Value)
            {
                throw new ArgumentException("При изменении дат у события, не может быть дата начала меньше даты завершения", nameof(endAt));
            }
        }

        return new UpdateEventDto(title, description, startAt, endAt);
    }
}
