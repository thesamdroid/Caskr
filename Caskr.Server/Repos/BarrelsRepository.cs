using Caskr.server.Models;
using Microsoft.EntityFrameworkCore;

namespace Caskr.server.Repos
{
    public interface IBarrelsRepository
    {
        Task<IEnumerable<Barrel>> GetBarrelsForCompanyAsync(int companyId);
        Task<IEnumerable<Barrel>> ForecastBarrelsAsync(int companyId, DateTime targetDate, int ageYears);
    }

    public class BarrelsRepository(CaskrDbContext dbContext) : IBarrelsRepository
    {
        public async Task<IEnumerable<Barrel>> GetBarrelsForCompanyAsync(int companyId)
        {
            return await dbContext.Barrels
                .Include(b => b.Order)
                .Where(b => b.CompanyId == companyId)
                .ToListAsync();
        }

        public async Task<IEnumerable<Barrel>> ForecastBarrelsAsync(int companyId, DateTime targetDate, int ageYears)
        {
            var cutoffDate = targetDate.AddYears(-ageYears);
            return await dbContext.Barrels
                .Include(b => b.Order)
                .Where(b => b.CompanyId == companyId && b.Order != null && b.Order.CreatedAt <= cutoffDate)
                .ToListAsync();
        }
    }
}
