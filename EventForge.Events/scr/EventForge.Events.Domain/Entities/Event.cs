using EventForge.Events.Domain.Exceptions;

namespace EventForge.Events.Domain.Entities;

/// <summary>
/// Событие сервиса управления
/// </summary>
public class Event
{
    /// <summary>
    /// Уникальный идентификатор события
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Название события
    /// </summary>
    public string Title { get; private set; }

    /// <summary>
    /// Описание события
    /// </summary>
    public string? Description { get; private set; }
    /// <summary>
    /// Дата начала события
    /// </summary>
    public DateTime StartAt { get; private set; }

    /// <summary>
    /// Дата завершения события
    /// </summary>
    public DateTime EndAt { get; private set; }

    /// <summary>
    /// Общее количество мест на событии
    /// </summary>
    public int TotalSeats { get; private set; }

    /// <summary>
    /// Текущее количество свободных мест
    /// </summary>
    public int AvailableSeats { get; private set; }

    /// <summary>
    /// Метод освобождения места от резервирования
    /// </summary>
    /// <param name="count">Количество мест для освобождения</param>
    public void ReleaseSeats(int count = 1)
    {
        if (count < 0)
            return;

        if (AvailableSeats + count >= TotalSeats)
            AvailableSeats = TotalSeats;
        else
            AvailableSeats += count;
    }

    /// <summary>
    /// Метод резервирования мест для событий
    /// </summary>
    /// <param name="count">Количество мест к резервированию</param>
    /// <returns></returns>
    public bool TryReserveSeats(int count = 1)
    {
        if (count < 0 || AvailableSeats < count)
            return false;

        AvailableSeats -= count;
        return true;
    }

    /// <summary>
    /// Обновление события
    /// </summary>
    /// <param name="title">Заголовок события</param>
    /// <param name="startDate">Дата начала события</param>
    /// <param name="endDate">Дата окончания события</param>
    /// <param name="description">Описание события</param>
    public void UpdateEvent(string title, DateTime startDate, DateTime endDate, string? description)
    {
        ValidateDates(startDate, endDate);

        Title = title;
        StartAt = startDate;
        EndAt = endDate;
        Description = description;
    }

    private Event(string title, DateTime startDate, DateTime endDate, int totalSeats,
        string? description = null)
    {
        Id = Guid.NewGuid();
        Title = title;
        StartAt = startDate;
        EndAt = endDate;
        TotalSeats = totalSeats;
        AvailableSeats = totalSeats;
        Description = description;

    }

    /// <summary>
    /// Метод создания события
    /// </summary>
    /// <param name="title">Заголовок события</param>
    /// <param name="startDate">Дата начала события</param>
    /// <param name="endDate">Дата окончания события</param>
    /// <param name="totalSeats">Общее количество мест</param>
    /// <param name="description">Описание события</param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public static Event Create(string title, DateTime startDate, DateTime endDate, int totalSeats, string? description = null)
    {
        ValidateDates(startDate, endDate);

        if (totalSeats <= 0)
            throw new ValidationCustomException(nameof(Event), Guid.Empty.ToString(), "Общее количество мест для события должно быть больше нуля.");

        return new Event(title, startDate, endDate, totalSeats, description);
    }

    private static void ValidateDates(DateTime start, DateTime end)
    {
        if (end.TimeOfDay == TimeSpan.Zero)
        {
            if (end.Date < start.Date)
                throw new ValidationCustomException(nameof(Event), Guid.Empty.ToString(), "Дата окончания не может быть раньше даты начала");
        }
        else
        {
            if (end < start)
            {
                throw new ValidationCustomException(nameof(Event), Guid.Empty.ToString(), "Дата окончания не может быть раньше даты начала");
            }
        }

    }
}
