using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Caskr.server.Models;
using Caskr.Server.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Caskr.Server.Services;

/// <summary>
///     Provides centralized management of QuickBooks chart of accounts mappings.
/// </summary>
public interface IQuickBooksAccountMappingService
{
    /// <summary>
    ///     Loads all account mappings for a company as a dictionary keyed by Caskr account type.
    /// </summary>
    /// <param name="companyId">The company identifier.</param>
    /// <returns>Dictionary of account mappings indexed by Caskr account type.</returns>
    Task<Dictionary<CaskrAccountType, ChartOfAccountsMapping>> LoadAccountMappingsAsync(int companyId);

    /// <summary>
    ///     Gets the QuickBooks account mapping for a specific Caskr account type.
    /// </summary>
    /// <param name="companyId">The company identifier.</param>
    /// <param name="accountType">The Caskr account type.</param>
    /// <returns>The mapping if found, otherwise null.</returns>
    Task<ChartOfAccountsMapping?> GetAccountMappingAsync(int companyId, CaskrAccountType accountType);

    /// <summary>
    ///     Validates that all required account mappings exist for a company.
    /// </summary>
    /// <param name="companyId">The company identifier.</param>
    /// <param name="requiredTypes">The account types that must be mapped.</param>
    /// <exception cref="InvalidOperationException">Thrown if any required mappings are missing.</exception>
    Task ValidateRequiredMappingsExistAsync(int companyId, params CaskrAccountType[] requiredTypes);
}

/// <summary>
///     Implementation of account mapping service.
/// </summary>
[AutoBind]
public sealed class QuickBooksAccountMappingService : IQuickBooksAccountMappingService
{
    private readonly CaskrDbContext _dbContext;
    private readonly ILogger<QuickBooksAccountMappingService> _logger;

    public QuickBooksAccountMappingService(
        CaskrDbContext dbContext,
        ILogger<QuickBooksAccountMappingService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Dictionary<CaskrAccountType, ChartOfAccountsMapping>> LoadAccountMappingsAsync(int companyId)
    {
        if (companyId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(companyId), "Company ID must be positive.");
        }

        var mappings = await _dbContext.ChartOfAccountsMappings
            .AsNoTracking()
            .Where(m => m.CompanyId == companyId)
            .ToListAsync();

        _logger.LogDebug(
            "Loaded {Count} account mappings for company {CompanyId}",
            mappings.Count,
            companyId);

        return mappings.ToDictionary(m => m.CaskrAccountType, m => m);
    }

    public async Task<ChartOfAccountsMapping?> GetAccountMappingAsync(int companyId, CaskrAccountType accountType)
    {
        if (companyId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(companyId), "Company ID must be positive.");
        }

        var mapping = await _dbContext.ChartOfAccountsMappings
            .AsNoTracking()
            .SingleOrDefaultAsync(m => m.CompanyId == companyId && m.CaskrAccountType == accountType);

        if (mapping is null)
        {
            _logger.LogWarning(
                "No account mapping found for company {CompanyId}, account type {AccountType}",
                companyId,
                accountType);
        }

        return mapping;
    }

    public async Task ValidateRequiredMappingsExistAsync(int companyId, params CaskrAccountType[] requiredTypes)
    {
        if (companyId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(companyId), "Company ID must be positive.");
        }

        if (requiredTypes is null || requiredTypes.Length == 0)
        {
            return;
        }

        var mappings = await LoadAccountMappingsAsync(companyId);

        foreach (var requiredType in requiredTypes)
        {
            if (!mappings.ContainsKey(requiredType))
            {
                var typeName = FormatAccountTypeName(requiredType);
                var errorMessage = $"{typeName} account mapping is missing for company {companyId}.";
                _logger.LogError(errorMessage);
                throw new InvalidOperationException(errorMessage);
            }
        }

        _logger.LogDebug(
            "Validated all required account mappings exist for company {CompanyId}",
            companyId);
    }

    private static string FormatAccountTypeName(CaskrAccountType accountType)
    {
        return accountType switch
        {
            CaskrAccountType.Cogs => "COGS",
            CaskrAccountType.WorkInProgress => "Work in Progress",
            CaskrAccountType.FinishedGoods => "Finished Goods",
            CaskrAccountType.RawMaterials => "Raw Materials",
            CaskrAccountType.Barrels => "Barrels",
            CaskrAccountType.Ingredients => "Ingredients",
            CaskrAccountType.Labor => "Labor",
            CaskrAccountType.Overhead => "Overhead",
            _ => accountType.ToString()
        };
    }
}
