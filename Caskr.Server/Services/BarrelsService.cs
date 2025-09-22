using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Caskr.server.Models;
using Caskr.server.Repos;
using Microsoft.AspNetCore.Http;

namespace Caskr.server.Services
{
    public class BarrelImportException : Exception
    {
        public BarrelImportException(string message) : base(message)
        {
        }
    }

    public class BatchRequiredException : BarrelImportException
    {
        public BatchRequiredException() : base("A batch ID is required to import barrels.")
        {
        }
    }

    public class RickhouseNotFoundException : BarrelImportException
    {
        public RickhouseNotFoundException(IEnumerable<string> names)
            : base($"Unable to find rickhouses: {string.Join(", ", names)}")
        {
        }
    }

    public class BatchNotFoundException : BarrelImportException
    {
        public BatchNotFoundException(int batchId)
            : base($"Batch {batchId} does not exist for this company.")
        {
        }
    }

    public class MashBillNotFoundException : BarrelImportException
    {
        public MashBillNotFoundException(int mashBillId)
            : base($"Mash bill {mashBillId} does not exist for this company.")
        {
        }
    }

    public record BarrelImportResult(int BatchId, int CreatedCount, bool CreatedNewBatch);

    internal sealed record BarrelImportRow(string Sku, string RickhouseName, string RickhouseKey);

    public interface IBarrelsService
    {
        Task<IEnumerable<Barrel>> GetBarrelsForCompanyAsync(int companyId);
        Task<IEnumerable<Barrel>> ForecastBarrelsAsync(int companyId, DateTime targetDate, int ageYears);
        Task<BarrelImportResult> ImportBarrelsAsync(int companyId, int userId, IFormFile file, int? batchId, int? mashBillId);
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

        public async Task<BarrelImportResult> ImportBarrelsAsync(int companyId, int userId, IFormFile file, int? batchId, int? mashBillId)
        {
            if (file is null || file.Length == 0)
            {
                throw new BarrelImportException("A CSV file is required.");
            }

            var rows = await ParseCsvAsync(file);
            if (rows.Count == 0)
            {
                throw new BarrelImportException("The CSV file did not contain any barrel rows.");
            }

            var rickhouseKeys = rows.Select(r => r.RickhouseKey).Distinct().ToList();
            var rickhouseIds = await repository.GetRickhouseIdsByNameAsync(companyId, rickhouseKeys)
                ?? new Dictionary<string, int>();
            var missingRickhouses = rickhouseKeys.Where(key => !rickhouseIds.ContainsKey(key)).ToList();
            if (missingRickhouses.Count > 0)
            {
                throw new RickhouseNotFoundException(rows
                    .Where(r => missingRickhouses.Contains(r.RickhouseKey))
                    .Select(r => r.RickhouseName)
                    .Distinct());
            }

            var resolvedBatchId = batchId;
            var createdNewBatch = false;

            if (resolvedBatchId.HasValue)
            {
                var exists = await repository.BatchExistsForCompanyAsync(companyId, resolvedBatchId.Value);
                if (!exists)
                {
                    throw new BatchNotFoundException(resolvedBatchId.Value);
                }
            }
            else
            {
                if (!mashBillId.HasValue)
                {
                    throw new BatchRequiredException();
                }

                var mashBillExists = await repository.MashBillExistsForCompanyAsync(companyId, mashBillId.Value);
                if (!mashBillExists)
                {
                    throw new MashBillNotFoundException(mashBillId.Value);
                }

                resolvedBatchId = await repository.CreateBatchAsync(companyId, mashBillId.Value);
                createdNewBatch = true;
            }

            var orderId = await repository.EnsureOrderForBatchAsync(companyId, userId, resolvedBatchId!.Value, rows.Count);

            var barrels = rows.Select(row => new Barrel
            {
                CompanyId = companyId,
                Sku = row.Sku,
                BatchId = resolvedBatchId.Value,
                OrderId = orderId,
                RickhouseId = rickhouseIds[row.RickhouseKey]
            });

            await repository.AddBarrelsAsync(barrels);

            return new BarrelImportResult(resolvedBatchId.Value, rows.Count, createdNewBatch);
        }

        private static async Task<List<BarrelImportRow>> ParseCsvAsync(IFormFile file)
        {
            using var stream = file.OpenReadStream();
            using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: false);
            var rows = new List<BarrelImportRow>();
            var isFirstRow = true;

            string? line;
            while ((line = await reader.ReadLineAsync()) != null)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                var parts = line.Split(',', StringSplitOptions.None);
                if (parts.Length < 2)
                {
                    throw new BarrelImportException("Each row must contain a SKU and rickhouse name.");
                }

                var skuValue = parts[0].Trim();
                var rickhouseValue = parts[1].Trim();

                if (isFirstRow && skuValue.Equals("sku", StringComparison.OrdinalIgnoreCase))
                {
                    isFirstRow = false;
                    continue;
                }

                isFirstRow = false;

                if (string.IsNullOrWhiteSpace(skuValue))
                {
                    throw new BarrelImportException("Each row must include a SKU value.");
                }

                if (string.IsNullOrWhiteSpace(rickhouseValue))
                {
                    throw new BarrelImportException("Each row must include a rickhouse name.");
                }

                var key = NormalizeRickhouseName(rickhouseValue);
                rows.Add(new BarrelImportRow(skuValue, rickhouseValue, key));
            }

            return rows;
        }

        private static string NormalizeRickhouseName(string name) => name.Trim().ToLowerInvariant();

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
