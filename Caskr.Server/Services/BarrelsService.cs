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
            var normalizedTargetDate = NormalizeToUtc(targetDate);
            return repository.ForecastBarrelsAsync(companyId, normalizedTargetDate, ageYears);
        }

        private static DateTime NormalizeToUtc(DateTime value)
        {
            return value.Kind switch
            {
                DateTimeKind.Utc => value,
                DateTimeKind.Local => value.ToUniversalTime(),
                _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
            };
        }
    }
}
