using EventForge.Users.Infrastructure.Entities;

using Microsoft.EntityFrameworkCore;

namespace EventForge.Users.Infrastructure.Context;

/// <summary>
/// Контекст базы данных Users-сервиса
/// </summary>
/// <param name="options">Параметры конфигурации DbContext</param>
public sealed class UsersDbContext(DbContextOptions<UsersDbContext> options) : DbContext(options)
{
    /// <summary>
    /// Пользователи
    /// </summary>
    public DbSet<UserEntity> Users => Set<UserEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("Users");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(UsersDbContext).Assembly);
    }
}
