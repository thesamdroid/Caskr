using Caskr.server.Models;
using Microsoft.EntityFrameworkCore;

namespace Caskr.server.Repos
{
    public interface IUserTypesRepository
    {
        Task<IEnumerable<UserType>> GetUserTypesAsync();
        Task<UserType?> GetUserTypeAsync(int id);
        Task<UserType> AddUserTypeAsync(UserType? userType);
        Task<UserType> UpdateUserTypeAsync(UserType userType);
        Task DeleteUserTypeAsync(int id);
    }

    public class UserTypesRepository(CaskrDbContext dbContext) : IUserTypesRepository
    {
        public async Task<IEnumerable<UserType>> GetUserTypesAsync()
        {
            return (await dbContext.UserTypes.ToListAsync())!;
        }
        public async Task<UserType?> GetUserTypeAsync(int id)
        {
            return await dbContext.UserTypes.FindAsync(id);
        }
        public async Task<UserType> AddUserTypeAsync(UserType? userType)
        {
            var createdUserType = await dbContext.UserTypes.AddAsync(userType);
            await dbContext.SaveChangesAsync();
            return createdUserType.Entity!;
        }
        public async Task<UserType> UpdateUserTypeAsync(UserType userType)
        {
            dbContext.Entry(userType).State = EntityState.Modified;
            await dbContext.SaveChangesAsync();
            return (await GetUserTypeAsync(userType.Id))!;
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
