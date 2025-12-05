using Caskr.server.Models;
using Microsoft.EntityFrameworkCore;

namespace Caskr.server.Repos
{
    public interface IUsersRepository
    {
        Task<IEnumerable<User>> GetUsersAsync();
        Task<User?> GetUserByIdAsync(int id);
        Task<User?> GetUserByEmailAsync(string email);
        Task<User> AddUserAsync(User? newUser);
        Task<User> UpdateUserAsync(User updatedUser);
        Task DeleteUserAsync(int id);
    }

    public class UsersRepository(CaskrDbContext dbContext) : IUsersRepository
    {
        private IQueryable<User> BuildUserQuery()
        {
            return dbContext.Users
                .Include(u => u.Company)
                .Select(u => new User
                {
                    Id = u.Id,
                    Name = u.Name,
                    Email = u.Email,
                    UserTypeId = u.UserTypeId,
                    CompanyId = u.CompanyId,
                    CompanyName = u.Company.CompanyName
                });
        }

        public async Task<IEnumerable<User>> GetUsersAsync()
        {
            return await BuildUserQuery().ToListAsync();
        }

        public async Task<User?> GetUserByIdAsync(int id)
        {
            return await BuildUserQuery().FirstOrDefaultAsync(u => u.Id == id);
        }

        public async Task<User?> GetUserByEmailAsync(string email)
        {
            return await BuildUserQuery().FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<User> AddUserAsync(User? newUser)
        {
            ArgumentNullException.ThrowIfNull(newUser);
            var createdUser = await dbContext.Users.AddAsync(newUser);
            await dbContext.SaveChangesAsync();
            return createdUser.Entity!;
        }

        public async Task<User> UpdateUserAsync(User updatedUser)
        {
            dbContext.Entry(updatedUser).State = EntityState.Modified;
            await dbContext.SaveChangesAsync();
            return (await GetUserByIdAsync(updatedUser.Id))!;
        }

        public async Task DeleteUserAsync(int id)
        {
            var user = await dbContext.Users.FindAsync(id);
            if (user != null)
            {
                dbContext.Users.Remove(user);
                await dbContext.SaveChangesAsync();
            }
        }
    }
}
