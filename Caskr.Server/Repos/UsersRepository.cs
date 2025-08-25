using Caskr.server.Models;
using Microsoft.EntityFrameworkCore;

namespace Caskr.server.Repos
{
    public interface IUsersRepository
    {
        Task<IEnumerable<User>> GetUsersAsync();
        Task<User?> GetUserAsync(int id);
        Task<User?> GetUserByEmailAsync(string email);
        Task<User> AddUserAsync(User? user);
        Task<User> UpdateUserAsync(User user);
        Task DeleteUserAsync(int id);
    }

    public class UsersRepository(CaskrDbContext dbContext) : IUsersRepository
    {
        public async Task<IEnumerable<User>> GetUsersAsync()
        {
            return await dbContext.Users
                .Include(u => u.Company)
                .Select(u => new User
                {
                    Id = u.Id,
                    Name = u.Name,
                    Email = u.Email,
                    UserTypeId = u.UserTypeId,
                    CompanyId = u.CompanyId,
                    CompanyName = u.Company.CompanyName
                })
                .ToListAsync();
        }
        public async Task<User?> GetUserAsync(int id)
        {
            return await dbContext.Users
                .Include(u => u.Company)
                .Select(u => new User
                {
                    Id = u.Id,
                    Name = u.Name,
                    Email = u.Email,
                    UserTypeId = u.UserTypeId,
                    CompanyId = u.CompanyId,
                    CompanyName = u.Company.CompanyName
                })
                .FirstOrDefaultAsync(u => u.Id == id);
        }
        public async Task<User?> GetUserByEmailAsync(string email)
        {
            return await dbContext.Users
                .Include(u => u.Company)
                .Select(u => new User
                {
                    Id = u.Id,
                    Name = u.Name,
                    Email = u.Email,
                    UserTypeId = u.UserTypeId,
                    CompanyId = u.CompanyId,
                    CompanyName = u.Company.CompanyName
                })
                .FirstOrDefaultAsync(u => u.Email == email);
        }
        public async Task<User> AddUserAsync(User? user)
        {
            var createdUser = await dbContext.Users.AddAsync(user);
            await dbContext.SaveChangesAsync();
            return createdUser.Entity!;
        }
        public async Task<User> UpdateUserAsync(User user)
        {
            dbContext.Entry(user).State = EntityState.Modified;
            await dbContext.SaveChangesAsync();
            return (await GetUserAsync(user.Id))!;
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
