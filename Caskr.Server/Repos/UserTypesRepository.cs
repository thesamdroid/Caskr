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
        private readonly CaskrDbContext _dbContext = dbContext;
        public async Task<IEnumerable<UserType?>> GetUserTypesAsync()
        {
            return await _dbContext.UserTypes.ToListAsync();
        }
        public async Task<UserType?> GetUserTypeAsync(int id)
        {
            return await _dbContext.UserTypes.FindAsync(id);
        }
        public async Task AddUserTypeAsync(UserType? userType)
        {
            await _dbContext.UserTypes.AddAsync(userType);
            await _dbContext.SaveChangesAsync();
        }
        public async Task UpdateUserTypeAsync(UserType userType)
        {
            _dbContext.Entry(userType).State = EntityState.Modified;
            await _dbContext.SaveChangesAsync();
        }
        public async Task DeleteUserTypeAsync(int id)
        {
            var userType = await _dbContext.UserTypes.FindAsync(id);
            if (userType != null)
            {
                _dbContext.UserTypes.Remove(userType);
                await _dbContext.SaveChangesAsync();
            }
        }
    }
}
