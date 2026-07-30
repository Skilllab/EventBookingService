namespace EventForge.Events.Application.DTO;

/// <summary>
/// DTO для создания события
/// </summary>
public class CreateEventDto
{
    /// <summary>
    /// Название события
    /// </summary>
    /// <remarks>
    /// Обязательное поле. Не может быть null, пустым или состоять из пробелов.
    /// </remarks>
    public string Title { get; }

    /// <summary>
    /// Дата начала события
    /// </summary>
    /// <remarks>
    /// Обязательное поле. Не может быть значением по умолчанию (default(DateTime)).
    /// Должна быть меньше даты окончания события.
    /// </remarks>
    public DateTime StartAt { get; }

    /// <summary>
    /// Дата завершения события
    /// </summary>
    /// <remarks>
    /// Обязательное поле. Не может быть значением по умолчанию (default(DateTime)).
    /// Должна быть больше даты начала события.
    /// </remarks>
    public DateTime EndAt { get; }

    /// <summary>
    /// Общее количество мест на событии
    /// </summary>
    /// <remarks>
    /// Обязательное поле. Должно быть больше нуля.
    /// </remarks>
    public int TotalSeats { get; }

    /// <summary>
    /// Описание события
    /// </summary>
    /// <remarks>
    /// Необязательное поле. Может быть null или пустой строкой.
    /// По умолчанию - пустая строка.
    /// </remarks>
    public string? Description { get; }

    /// <summary>
    /// Конструктор DTO для создания события
    /// </summary>
    /// <param name="title">Название события (обязательное, не может быть null или пустым)</param>
    /// <param name="startAt">Дата начала события (обязательная, не может быть default)</param>
    /// <param name="endAt">Дата завершения события (обязательная, должна быть больше startAt)</param>
    /// <param name="totalSeats">Общее количество мест (должно быть больше 0)</param>
    /// <param name="description">Описание события (необязательное, по умолчанию пустая строка)</param>
    /// <exception cref="ArgumentException">
    /// Выбрасывается, если:
    /// - title пустой, null или состоит из пробелов
    /// - startAt или endAt равны default(DateTime)
    /// - endAt меньше startAt
    /// - totalSeats меньше 1
    /// </exception>
    public CreateEventDto(
        string title,
        DateTime startAt,
        DateTime endAt,
        int totalSeats,
        string? description = "")
    {
        // Проверка Title
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Наименование для события обязательно для заполнения.", nameof(title));

        // Проверка StartAt
        if (startAt == default)
            throw new ArgumentException("Дата начала события должна быть задана.", nameof(startAt));

        // Проверка EndAt
        if (endAt == default)
            throw new ArgumentException("Дата окончания события должна быть задана.", nameof(endAt));

        // Проверка: EndAt должна быть больше StartAt
        if (endAt < startAt)
            throw new ArgumentException("Дата окончания события должна быть больше даты начала.", nameof(endAt));

        // Проверка TotalSeats
        if (totalSeats < 1)
            throw new ArgumentException("Общее количество мест для события должно быть больше нуля.", nameof(totalSeats));

        Title = title;
        StartAt = startAt;
        EndAt = endAt;
        TotalSeats = totalSeats;
        Description = description;
    }
}