using EventForge.Users.Application.Interfaces;
using EventForge.Users.Domain.Entities;
using EventForge.Users.Infrastructure.Context;
using EventForge.Users.Infrastructure.Mapping;

using Microsoft.EntityFrameworkCore;

namespace EventForge.Users.Infrastructure.Repositories;

/// <summary>
/// Репозиторий пользователей
/// </summary>
/// <param name="factory">Фабрика DbContext</param>
public class UserRepository(IDbContextFactory<UsersDbContext> factory) : IUserRepository
{
    public async Task<User?> GetByLoginAsync(string login)
    {
        await using var context = await factory.CreateDbContextAsync();
        var user = await context.Users.FirstOrDefaultAsync(u => u.Login == login);

        return user?.ToDomain();
    }

    public async Task AddAsync(User user)
    {
        await using var context = await factory.CreateDbContextAsync();

        await context.Users.AddAsync(user.ToEntity());
        await context.SaveChangesAsync();
    }

    public async Task<bool> ExistsAsync(string login)
    {
        await using var context = await factory.CreateDbContextAsync();
        return await context.Users.AnyAsync(u => u.Login == login);
    }
}
