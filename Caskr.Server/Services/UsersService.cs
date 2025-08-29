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

    public class UsersService(IUsersRepository usersRepository, IKeycloakClient keycloakClient) : IUsersService
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
            if (newUser is null)
            {
                throw new ArgumentNullException(nameof(newUser));
            }

            var existing = await usersRepository.GetUserByEmailAsync(newUser.Email);
            if (existing is not null)
            {
                throw new InvalidOperationException("User already exists");
            }

            await keycloakClient.CreateUserAsync(newUser, newUser.TemporaryPassword);
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
