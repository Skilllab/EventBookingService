namespace EventForge.Events.Application.DTO;

/// <summary>
/// Класс служит транслятором данных
/// </summary>
/// <param name="Id">Уникальный идентификатор события</param>
/// <param name="Title">Название события</param>
/// <param name="Description">Описание события</param>
/// <param name="StartAt">Дата начала события</param>
/// <param name="EndAt">Дата завершения события</param>
/// <param name="TotalSeats">Общее количество мест на событии</param>
/// <param name="AvailableSeats">Текущее количество свободных мест</param>
public record EventDTO(
    Guid Id,
    string Title,
    string? Description,
    DateTime StartAt,
    DateTime EndAt,
    int TotalSeats,
    int AvailableSeats);