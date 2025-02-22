using Caskr.server.Models;
using Caskr.server.Repos;

namespace Caskr.server.Services
{
    public interface IUserTypesService
    {
        Task<IEnumerable<UserType>> GetUserTypesAsync();
        Task<UserType?> GetUserTypeAsync(int id);
        Task<UserType> AddUserTypeAsync(UserType? userType);
        Task<UserType> UpdateUserTypeAsync(UserType userType);
        Task DeleteUserTypeAsync(int id);
    }

    public class UserTypesService(IUserTypesRepository userTypesRepository) : IUserTypesService
    {
        public async Task<IEnumerable<UserType>> GetUserTypesAsync()
        {
            return await userTypesRepository.GetUserTypesAsync();
        }

        public async Task<UserType?> GetUserTypeAsync(int id)
        {
            return await userTypesRepository.GetUserTypeAsync(id);
        }

        public async Task<UserType> AddUserTypeAsync(UserType? userType)
        {
            return await userTypesRepository.AddUserTypeAsync(userType);
        }

        public async Task<UserType> UpdateUserTypeAsync(UserType userType)
        {
            return await userTypesRepository.UpdateUserTypeAsync(userType);
        }

        public async Task DeleteUserTypeAsync(int id)
        {
            await userTypesRepository.DeleteUserTypeAsync(id);
        }
    }
}
