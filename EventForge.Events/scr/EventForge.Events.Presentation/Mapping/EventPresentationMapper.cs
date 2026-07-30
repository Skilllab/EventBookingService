using EventForge.Events.Application.DTO;
using EventForge.Events.Presentation.DTO;

namespace EventForge.Events.Presentation.Mapping;

/// <summary>
/// Маппер для преобразования моделей между слоями Presentation и Application
/// </summary>
public static class EventPresentationMapper
{
    /// <summary>
    /// Преобразует входной запрос создания события (Presentation) в DTO бизнес-логики (Application)
    /// </summary>
    /// <param name="request">Запрос создания события из запроса.</param>
    public static CreateEventDto ToAppDto(this CreateEventRequest request) =>
        new(
            title: request.Title,
            description: request.Description,
            startAt: request.StartAt,
            endAt: request.EndAt,
            totalSeats: request.TotalSeats
        );

    /// <summary>
    /// Преобразует выходной DTO бизнес-логики (Application) в ответ клиенту (Presentation)
    /// </summary>
    /// <param name="dto">DTO события из слоя Application.</param>
    public static EventResponse ToWebDto(this EventDTO dto) =>
        new(
            Id: dto.Id,
            Title: dto.Title,
            Description: dto.Description,
            StartAt: dto.StartAt,
            EndAt: dto.EndAt,
            TotalSeats: dto.TotalSeats,
            // Значение маппится из DTO приложения, где оно было рассчитано бизнес-логикой
            AvailableSeats: dto.AvailableSeats
        );

    /// <summary>
    /// Преобразует входной запрос обновления события (Presentation) в DTO бизнес-логики (Application)
    /// </summary>
    /// <param name="request">Запрос обновления события</param>
    public static UpdateEventDto ToAppDto(this UpdateEventRequest request) =>
        UpdateEventDto.Create
        (
            title: request.Title,
            description: request.Description,
            startAt: request.StartAt,
            endAt: request.EndAt
        );

    /// <summary>
    /// Преобразует входной запрос фильтрации (Presentation) в DTO бизнес-логики (Application)
    /// </summary>
    /// <param name="request">Запрос фильтрации</param>
    public static EventsFilterDTO ToAppDto(this EventsFilterRequest request) =>
        new(
            title: request.Title,
            from: request.From,
            to: request.To,
            page: request.Page,
            pageSize: request.PageSize
        );



    /// <summary>
    /// Преобразует выходной DTO бизнес-логики (Application) в ответ клиенту (Presentation)
    /// </summary>
    /// <param name="dto">DTO поиска с пагинацией</param>
    public static PaginatedResultResponse ToWebDto(this PaginatedResultDTO dto) =>
        new(
            EventsTotalCount: dto.EventsTotalCount,
            Events: dto.Events.Select(e => e.ToWebDto()).ToList(),
            CurrentPageNumber: dto.CurrentPageNumber,
            EventsCountOnCurrentPage: dto.EventsCountOnCurrentPage);


    /// <summary>
    /// Преобразует выходной DTO бизнес-логики (Application) в ответ клиенту (Presentation) для топ-10 событий
    /// </summary>
    /// <param name="dto">DTO топ-10 событий из слоя Application</param>
    /// <returns>Ответ клиенту с топ-10 событиями</returns>
    public static PaginatedResultTop10Response ToWebDto(this PaginatedResultTop10DTO dto) =>
        new(
            Events: dto.Events.Select(e => e.ToWebDto()).ToList());



}
