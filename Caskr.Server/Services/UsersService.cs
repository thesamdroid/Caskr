using Caskr.server.Models;
using Caskr.server.Repos;

namespace Caskr.server.Services
{
    public interface IUsersService
    {
        Task<IEnumerable<User?>> GetUsersAsync();
        Task<User?> GetUserAsync(int id);
        Task AddUserAsync(User? user);
        Task UpdateUserAsync(User user);
        Task DeleteUserAsync(int id);
    }

    public class UsersService(IUsersRepository usersRepository) : IUsersService
    {
        public async Task<IEnumerable<User?>> GetUsersAsync()
        {
            return await usersRepository.GetUsersAsync();
        }
        public async Task<User?> GetUserAsync(int id)
        {
            return await usersRepository.GetUserAsync(id);
        }
        public async Task AddUserAsync(User? user)
        {
            await usersRepository.AddUserAsync(user);
        }
        public async Task UpdateUserAsync(User user)
        {
            await usersRepository.UpdateUserAsync(user);
        }
        public async Task DeleteUserAsync(int id)
        {
            await usersRepository.DeleteUserAsync(id);
        }
    }
}
