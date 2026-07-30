namespace EventForge.Events.Presentation.DTO;

/// <summary>
/// Перечень событий с фильтрацией и пагинацией
/// </summary>
/// <param name="EventsTotalCount">Общее количество событий, найденных по фильтру</param>
/// <param name="Events">Список событий</param>
/// <param name="CurrentPageNumber">Номер текущей страницы</param>
/// <param name="EventsCountOnCurrentPage">Количество элементов на текущей странице</param>
public record PaginatedResultResponse(
    long EventsTotalCount,
    List<EventResponse> Events,
    int CurrentPageNumber,
    int EventsCountOnCurrentPage
);