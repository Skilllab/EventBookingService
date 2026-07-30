namespace EventForge.Events.Domain.Entities
{

    /// <summary>
    /// Результат пагинации
    /// </summary>
    /// <typeparam name="T">Тип сущности в пагинации</typeparam>
    /// <param name="Items">Сущности в пагинации</param>
    public record Top10PagedResult<T>(List<T> Items);
}
