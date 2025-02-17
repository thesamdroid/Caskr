using Caskr.server.Models;
using Microsoft.EntityFrameworkCore;

namespace Caskr.server.Repos
{
    public interface IStatusRepository
    {
        Task<IEnumerable<Status?>> GetStatusesAsync();
        Task<Status?> GetStatusAsync(int id);
        Task AddStatusAsync(Status? status);
        Task UpdateStatusAsync(Status status);
        Task DeleteStatusAsync(int id);
    }

    public class StatusRepository(CaskrDbContext dbContext) : IStatusRepository
    {
        public async Task<IEnumerable<Status?>> GetStatusesAsync()
        {
            return await dbContext.Statuses.ToListAsync();
        }

        public async Task<Status?> GetStatusAsync(int id)
        {
            return await dbContext.Statuses.FindAsync(id);
        }

        public async Task AddStatusAsync(Status? status)
        {
            await dbContext.Statuses.AddAsync(status);
            await dbContext.SaveChangesAsync();
        }

        public async Task UpdateStatusAsync(Status status)
        {
            dbContext.Entry(status).State = EntityState.Modified;
            await dbContext.SaveChangesAsync();
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
