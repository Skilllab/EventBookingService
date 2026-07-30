namespace EventForge.Events.Application.DTO;

/// <summary>
/// Фильтр получения событий (слой Application)
/// </summary>
/// <remarks>
/// DTO для фильтрации и пагинации событий. Содержит критерии поиска и параметры постраничного вывода.
/// Все проверки выполняются в конструкторе, при невалидных данных выбрасывается ValidationException.
/// </remarks>
public class EventsFilterDTO
{
    /// <summary>
    /// Название события для поиска
    /// </summary>
    /// <remarks>
    /// Используется для поиска событий по названию (частичное совпадение).
    /// Максимальная длина - 100 символов.
    /// </remarks>
    public string? Title { get; }

    /// <summary>
    /// Дата начала события
    /// </summary>
    /// <remarks>
    /// Задает нижнюю границу фильтрации по дате начала события.
    /// </remarks>
    public DateTime? From { get; }

    /// <summary>
    /// Дата завершения события
    /// </summary>
    /// <remarks>
    /// Задает верхнюю границу фильтрации по дате завершения события.
    /// Не может быть меньше даты From.
    /// </remarks>
    public DateTime? To { get; }

    /// <summary>
    /// Страница, которую необходимо вернуть
    /// </summary>
    /// <remarks>
    /// Номер запрашиваемой страницы. Нумерация начинается с 1.
    /// Значение должно быть больше 0. По умолчанию: 1.
    /// </remarks>
    public int Page { get; }

    /// <summary>
    /// Количество элементов на странице
    /// </summary>
    /// <remarks>
    /// Размер одной страницы результатов. Допустимый диапазон: от 1 до 100 элементов.
    /// По умолчанию: 10.
    /// </remarks>
    public int PageSize { get; }

    /// <summary>
    /// Конструктор фильтра событий
    /// </summary>
    /// <param name="title">Название события для поиска (необязательный, максимальная длина 100 символов)</param>
    /// <param name="from">Нижняя граница фильтрации по дате начала (необязательный)</param>
    /// <param name="to">Верхняя граница фильтрации по дате завершения (необязательный)</param>
    /// <param name="page">Номер страницы (по умолчанию: 1, должен быть больше 0)</param>
    /// <param name="pageSize">Размер страницы (по умолчанию: 10, допустимый диапазон: 1-100)</param>
    /// <exception cref="ArgumentException">
    /// Выбрасывается в следующих случаях:
    /// - Длина Title превышает 100 символов
    /// - Page меньше 1
    /// - PageSize не входит в диапазон от 1 до 100
    /// - To меньше From (при условии, что обе даты указаны)
    /// </exception>
    public EventsFilterDTO(
        string? title = null,
        DateTime? from = null,
        DateTime? to = null,
        int page = 1,
        int pageSize = 10)
    {
        if (title is not null && title.Length > 100)
        {
            throw new ArgumentException("Название для поиска слишком длинное", nameof(title));
        }

        if (page < 1)
        {
            throw new ArgumentException("Номер страницы должен быть больше 0", nameof(page));
        }

        if (pageSize is < 1 or > 100)
        {
            throw new ArgumentException("Размер страницы должен быть от 1 до 100 элементов", nameof(pageSize));
        }

        if (from.HasValue && to.HasValue && to < from)
        {
            throw new ArgumentException("Дата завершения не может быть раньше даты начала", nameof(to));
        }

        Title = title;
        From = from;
        To = to;
        Page = page;
        PageSize = pageSize;
    }
}
