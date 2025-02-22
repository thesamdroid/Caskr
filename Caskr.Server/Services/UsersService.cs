using Caskr.server.Models;
using Caskr.server.Repos;

namespace Caskr.server.Services
{
    public interface IUsersService
    {
        Task<IEnumerable<User>> GetUsersAsync();
        Task<User?> GetUserAsync(int id);
        Task<User> AddUserAsync(User? user);
        Task<User> UpdateUserAsync(User user);
        Task DeleteUserAsync(int id);
    }

    public class UsersService(IUsersRepository usersRepository) : IUsersService
    {
        public async Task<IEnumerable<User>> GetUsersAsync()
        {
            return await usersRepository.GetUsersAsync();
        }
        public async Task<User?> GetUserAsync(int id)
        {
            return await usersRepository.GetUserAsync(id);
        }
        public async Task<User> AddUserAsync(User? user)
        {
            return await usersRepository.AddUserAsync(user);
        }
        public async Task<User> UpdateUserAsync(User user)
        {
            return await usersRepository.UpdateUserAsync(user);
        }
        public async Task DeleteUserAsync(int id)
        {
            await usersRepository.DeleteUserAsync(id);
        }
    }
}
