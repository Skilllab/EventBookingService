namespace EventForge.Events.Presentation.DTO;

/// <summary>
/// DTO для ответов (информация о событии)
/// </summary>
/// <param name="Id">Уникальный идентификатор события</param>
/// <param name="Title">Название события</param>
/// <param name="StartAt">Дата начала события</param>
/// <param name="EndAt">Дата завершения события</param>
/// <param name="TotalSeats">Общее количество мест на событии</param>
/// <param name="AvailableSeats">Текущее количество свободных мест</param>
/// <param name="Description">Описание события</param>
public record EventResponse(
    Guid Id,
    string Title,
    DateTime StartAt,
    DateTime EndAt,
    int TotalSeats,
    int AvailableSeats,
    string? Description = null
);