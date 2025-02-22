using Caskr.server.Models;
using Caskr.server.Repos;

namespace Caskr.server.Services
{
    public interface IStatusService
    {
        Task<IEnumerable<Status>> GetStatusesAsync();
        Task<Status?> GetStatusAsync(int id);
        Task<Status> AddStatusAsync(Status? status);
        Task<Status> UpdateStatusAsync(Status status);
        Task DeleteStatusAsync(int id);
    }

    public class StatusService(IStatusRepository statusRepository) : IStatusService
    {
        public async Task<IEnumerable<Status>> GetStatusesAsync()
        {
            return await statusRepository.GetStatusesAsync();
        }
        public async Task<Status?> GetStatusAsync(int id)
        {
            return await statusRepository.GetStatusAsync(id);
        }
        public async Task<Status> AddStatusAsync(Status? status)
        {
            return await statusRepository.AddStatusAsync(status);
        }
        public async Task<Status> UpdateStatusAsync(Status status)
        {
            return await statusRepository.UpdateStatusAsync(status);
        }
        public async Task DeleteStatusAsync(int id)
        {
            await statusRepository.DeleteStatusAsync(id);
        }
    }
}
