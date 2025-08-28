using Caskr.server.Models;
using Caskr.server.Repos;

namespace Caskr.server.Services
{
    public interface IBarrelsService
    {
        Task<IEnumerable<Barrel>> GetBarrelsForCompanyAsync(int companyId);
        Task<IEnumerable<Barrel>> ForecastBarrelsAsync(int companyId, DateTime targetDate, int ageYears);
    }

    public class BarrelsService(IBarrelsRepository repository) : IBarrelsService
    {
        public Task<IEnumerable<Barrel>> GetBarrelsForCompanyAsync(int companyId)
        {
            return repository.GetBarrelsForCompanyAsync(companyId);
        }

        public Task<IEnumerable<Barrel>> ForecastBarrelsAsync(int companyId, DateTime targetDate, int ageYears)
        {
            return repository.ForecastBarrelsAsync(companyId, targetDate, ageYears);
        }
    }
}
