namespace EventForge.Events.Application.DTO;

/// <remarks>
/// Перечень событий ТОП 10
/// </remarks>
/// <param name="Events">Список событий</param>
public record PaginatedResultTop10DTO(List<EventDTO> Events);
