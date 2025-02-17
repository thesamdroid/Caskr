using Caskr.server.Models;
using Caskr.server.Repos;

namespace Caskr.server.Services
{
    public interface IUserTypesService
    {
        Task<IEnumerable<UserType?>> GetUserTypesAsync();
        Task<UserType?> GetUserTypeAsync(int id);
        Task AddUserTypeAsync(UserType? userType);
        Task UpdateUserTypeAsync(UserType userType);
        Task DeleteUserTypeAsync(int id);
    }

    public class UserTypesService(IUserTypesRepository userTypesRepository) : IUserTypesService
    {
        public async Task<IEnumerable<UserType?>> GetUserTypesAsync()
        {
            return await userTypesRepository.GetUserTypesAsync();
        }

        public async Task<UserType?> GetUserTypeAsync(int id)
        {
            return await userTypesRepository.GetUserTypeAsync(id);
        }

        public async Task AddUserTypeAsync(UserType? userType)
        {
            await userTypesRepository.AddUserTypeAsync(userType);
        }

        public async Task UpdateUserTypeAsync(UserType userType)
        {
            await userTypesRepository.UpdateUserTypeAsync(userType);
        }

        public async Task DeleteUserTypeAsync(int id)
        {
            await userTypesRepository.DeleteUserTypeAsync(id);
        }
    }
}
