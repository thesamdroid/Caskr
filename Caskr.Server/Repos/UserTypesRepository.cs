using Caskr.server.Models;
using Microsoft.EntityFrameworkCore;

namespace Caskr.server.Repos
{
    public interface IUserTypesRepository
    {
        Task<IEnumerable<UserType?>> GetUserTypesAsync();
        Task<UserType?> GetUserTypeAsync(int id);
        Task AddUserTypeAsync(UserType? userType);
        Task UpdateUserTypeAsync(UserType userType);
        Task DeleteUserTypeAsync(int id);
    }

    public class UserTypesRepository(CaskrDbContext dbContext) : IUserTypesRepository
    {
        public async Task<IEnumerable<UserType?>> GetUserTypesAsync()
        {
            return await dbContext.UserTypes.ToListAsync();
        }
        public async Task<UserType?> GetUserTypeAsync(int id)
        {
            return await dbContext.UserTypes.FindAsync(id);
        }
        public async Task AddUserTypeAsync(UserType? userType)
        {
            await dbContext.UserTypes.AddAsync(userType);
            await dbContext.SaveChangesAsync();
        }
        public async Task UpdateUserTypeAsync(UserType userType)
        {
            dbContext.Entry(userType).State = EntityState.Modified;
            await dbContext.SaveChangesAsync();
        }
        public async Task DeleteUserTypeAsync(int id)
        {
            var userType = await dbContext.UserTypes.FindAsync(id);
            if (userType != null)
            {
                dbContext.UserTypes.Remove(userType);
                await dbContext.SaveChangesAsync();
            }
        }
    }
}
