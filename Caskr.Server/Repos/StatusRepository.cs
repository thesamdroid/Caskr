using Caskr.server.Models;
using Microsoft.EntityFrameworkCore;

namespace Caskr.server.Repos
{
    public interface IStatusRepository
    {
        Task<IEnumerable<Status>> GetStatusesAsync();
        Task<Status> GetStatusAsync(int id);
        Task AddStatusAsync(Status status);
        Task UpdateStatusAsync(Status status);
        Task DeleteStatusAsync(int id);
    }

    public class StatusRepository(CaskrDbContext dbContext) : IStatusRepository
    {
        private readonly CaskrDbContext _dbContext = dbContext;

        public async Task<IEnumerable<Status>> GetStatusesAsync()
        {
            return await _dbContext.Statuses.ToListAsync();
        }

        public async Task<Status> GetStatusAsync(int id)
        {
            return await _dbContext.Statuses.FindAsync(id);
        }

        public async Task AddStatusAsync(Status status)
        {
            await _dbContext.Statuses.AddAsync(status);
            await _dbContext.SaveChangesAsync();
        }

        public async Task UpdateStatusAsync(Status status)
        {
            _dbContext.Entry(status).State = EntityState.Modified;
            await _dbContext.SaveChangesAsync();
        }

        public async Task DeleteStatusAsync(int id)
        {
            var status = await _dbContext.Statuses.FindAsync(id);
            if (status != null)
            {
                _dbContext.Statuses.Remove(status);
                await _dbContext.SaveChangesAsync();
            }
        } 
    }
}
