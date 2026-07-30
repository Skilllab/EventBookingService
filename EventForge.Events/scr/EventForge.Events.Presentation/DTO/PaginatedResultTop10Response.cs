namespace EventForge.Events.Presentation.DTO
{


    /// <summary>
    /// Перечень событий с фильтрацией и пагинацией
    /// </summary>
    /// <param name="Events">Список событий</param>
    public record PaginatedResultTop10Response(List<EventResponse> Events);
}
