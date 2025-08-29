using Caskr.server.Models;
using Caskr.server.Repos;
using UserTypeModel = Caskr.server.Models.UserType;

namespace Caskr.server.Services
{
    public interface IUserTypesService
    {
        Task<IEnumerable<UserTypeModel>> GetUserTypesAsync();
        Task<UserTypeModel?> GetUserTypeAsync(int id);
        Task<UserTypeModel> AddUserTypeAsync(UserTypeModel? userType);
        Task<UserTypeModel> UpdateUserTypeAsync(UserTypeModel userType);
        Task DeleteUserTypeAsync(int id);
    }

    public class UserTypesService(IUserTypesRepository userTypesRepository) : IUserTypesService
    {
        public async Task<IEnumerable<UserTypeModel>> GetUserTypesAsync()
        {
            return await userTypesRepository.GetUserTypesAsync();
        }

        public async Task<UserTypeModel?> GetUserTypeAsync(int id)
        {
            return await userTypesRepository.GetUserTypeAsync(id);
        }

        public async Task<UserTypeModel> AddUserTypeAsync(UserTypeModel? userType)
        {
            return await userTypesRepository.AddUserTypeAsync(userType);
        }

        public async Task<UserTypeModel> UpdateUserTypeAsync(UserTypeModel userType)
        {
            return await userTypesRepository.UpdateUserTypeAsync(userType);
        }

        public async Task DeleteUserTypeAsync(int id)
        {
            await userTypesRepository.DeleteUserTypeAsync(id);
        }
    }
}
