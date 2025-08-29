using Caskr.server.Models;
using Microsoft.EntityFrameworkCore;
using UserTypeModel = Caskr.server.Models.UserType;

namespace Caskr.server.Repos
{
    public interface IUserTypesRepository
    {
        Task<IEnumerable<UserTypeModel>> GetUserTypesAsync();
        Task<UserTypeModel?> GetUserTypeAsync(int id);
        Task<UserTypeModel> AddUserTypeAsync(UserTypeModel? userType);
        Task<UserTypeModel> UpdateUserTypeAsync(UserTypeModel userType);
        Task DeleteUserTypeAsync(int id);
    }

    public class UserTypesRepository(CaskrDbContext dbContext) : IUserTypesRepository
    {
        public async Task<IEnumerable<UserTypeModel>> GetUserTypesAsync()
        {
            return (await dbContext.UserTypes.ToListAsync())!;
        }
        public async Task<UserTypeModel?> GetUserTypeAsync(int id)
        {
            return await dbContext.UserTypes.FindAsync(id);
        }
        public async Task<UserTypeModel> AddUserTypeAsync(UserTypeModel? userType)
        {
            var createdUserType = await dbContext.UserTypes.AddAsync(userType);
            await dbContext.SaveChangesAsync();
            return createdUserType.Entity!;
        }
        public async Task<UserTypeModel> UpdateUserTypeAsync(UserTypeModel userType)
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
