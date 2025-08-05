using Caskr.server.Models;
using Microsoft.EntityFrameworkCore;

namespace Caskr.server.Repos
{
    public interface IStatusRepository
    {
        Task<IEnumerable<Status>> GetStatusesAsync();
        Task<Status?> GetStatusAsync(int id);
        Task<Status> AddStatusAsync(Status? status);
        Task<Status> UpdateStatusAsync(Status status);
        Task DeleteStatusAsync(int id);
    }

    public class StatusRepository(CaskrDbContext dbContext) : IStatusRepository
    {
        public async Task<IEnumerable<Status>> GetStatusesAsync()
        {
            return (await dbContext.Statuses
                .Include(s => s.StatusTasks)
                .ToListAsync())!;
        }

        public async Task<Status?> GetStatusAsync(int id)
        {
            return await dbContext.Statuses
                .Include(s => s.StatusTasks)
                .SingleOrDefaultAsync(s => s.Id == id);
        }

        public async Task<Status> AddStatusAsync(Status? status)
        {
            var createdStatus = await dbContext.Statuses.AddAsync(status);
            await dbContext.SaveChangesAsync();
            return createdStatus.Entity!;
        }

        public async Task<Status> UpdateStatusAsync(Status status)
        {
            dbContext.Entry(status).State = EntityState.Modified;
            await dbContext.SaveChangesAsync();
            return (await GetStatusAsync(status.Id))!;
        }

        public async Task DeleteStatusAsync(int id)
        {
            var status = await dbContext.Statuses.FindAsync(id);
            if (status != null)
            {
                dbContext.Statuses.Remove(status);
                await dbContext.SaveChangesAsync();
            }
        } 
    }
}
