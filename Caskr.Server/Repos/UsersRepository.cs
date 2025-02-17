using Caskr.server.Models;
using Microsoft.EntityFrameworkCore;

namespace Caskr.server.Repos
{
    public interface IUsersRepository
    {
        Task<IEnumerable<User?>> GetUsersAsync();
        Task<User?> GetUserAsync(int id);
        Task AddUserAsync(User? user);
        Task UpdateUserAsync(User user);
        Task DeleteUserAsync(int id);
    }

    public class UsersRepository(CaskrDbContext dbContext) : IUsersRepository
    {
        private readonly CaskrDbContext _dbContext = dbContext;
        public async Task<IEnumerable<User?>> GetUsersAsync()
        {
            return await _dbContext.Users.ToListAsync();
        }
        public async Task<User?> GetUserAsync(int id)
        {
            return await _dbContext.Users.FindAsync(id);
        }
        public async Task AddUserAsync(User? user)
        {
            await _dbContext.Users.AddAsync(user);
            await _dbContext.SaveChangesAsync();
        }
        public async Task UpdateUserAsync(User user)
        {
            _dbContext.Entry(user).State = EntityState.Modified;
            await _dbContext.SaveChangesAsync();
        }
        public async Task DeleteUserAsync(int id)
        {
            var user = await _dbContext.Users.FindAsync(id);
            if (user != null)
            {
                _dbContext.Users.Remove(user);
                await _dbContext.SaveChangesAsync();
            }
        }
    }
}
