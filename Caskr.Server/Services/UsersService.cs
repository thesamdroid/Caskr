using Caskr.server.Models;
using Caskr.server.Repos;

namespace Caskr.server.Services
{
    public interface IUsersService
    {
        Task<IEnumerable<User>> GetUsersAsync();
        Task<User?> GetUserByIdAsync(int id);
        Task<User?> GetUserByEmailAsync(string email);
        Task<User> AddUserAsync(User? newUser);
        Task<User> UpdateUserAsync(User updatedUser);
        Task DeleteUserAsync(int id);
    }

    public class UsersService(IUsersRepository usersRepository) : IUsersService
    {
        public async Task<IEnumerable<User>> GetUsersAsync()
        {
            return await usersRepository.GetUsersAsync();
        }
        public async Task<User?> GetUserByIdAsync(int id)
        {
            return await usersRepository.GetUserByIdAsync(id);
        }
        public async Task<User?> GetUserByEmailAsync(string email)
        {
            return await usersRepository.GetUserByEmailAsync(email);
        }
        public async Task<User> AddUserAsync(User? newUser)
        {
            return await usersRepository.AddUserAsync(newUser);
        }
        public async Task<User> UpdateUserAsync(User updatedUser)
        {
            return await usersRepository.UpdateUserAsync(updatedUser);
        }
        public async Task DeleteUserAsync(int id)
        {
            await usersRepository.DeleteUserAsync(id);
        }
    }
}
