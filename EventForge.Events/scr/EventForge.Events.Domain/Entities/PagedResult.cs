namespace EventForge.Events.Domain.Entities;

/// <summary>
/// Результат пагинации
/// </summary>
/// <typeparam name="T">Тип сущности в пагинации</typeparam>
/// <param name="Items">Сущности в пагинации</param>
/// <param name="TotalCount">Количество сущностей в пагинации</param>
public record PagedResult<T>(List<T> Items, long TotalCount);