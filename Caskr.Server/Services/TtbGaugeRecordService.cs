using Caskr.server.Models;
using Microsoft.EntityFrameworkCore;

namespace Caskr.server.Services;

public interface ITtbGaugeRecordService
{
    Task<TtbGaugeRecord> CreateGaugeRecordAsync(
        int barrelId,
        TtbGaugeType gaugeType,
        decimal proof,
        decimal temperatureFahrenheit,
        decimal wineGallons,
        int? gaugedByUserId = null,
        string? notes = null);

    Task<IEnumerable<TtbGaugeRecord>> GetGaugeRecordsForBarrelAsync(int barrelId);

    Task<IEnumerable<TtbGaugeRecord>> GetGaugeRecordsForCompanyAsync(int companyId, DateTime? startDate = null, DateTime? endDate = null);

    Task<TtbGaugeRecord?> GetGaugeRecordByIdAsync(int id);

    Task<TtbGaugeRecord> UpdateGaugeRecordAsync(
        int id,
        decimal proof,
        decimal temperatureFahrenheit,
        decimal wineGallons,
        string? notes = null);

    Task DeleteGaugeRecordAsync(int id);
}

public class TtbGaugeRecordService : ITtbGaugeRecordService
{
    private readonly CaskrDbContext _context;
    private readonly ITtbTemperatureCorrectionService _temperatureCorrectionService;

    public TtbGaugeRecordService(
        CaskrDbContext context,
        ITtbTemperatureCorrectionService temperatureCorrectionService)
    {
        _context = context;
        _temperatureCorrectionService = temperatureCorrectionService;
    }

    public async Task<TtbGaugeRecord> CreateGaugeRecordAsync(
        int barrelId,
        TtbGaugeType gaugeType,
        decimal proof,
        decimal temperatureFahrenheit,
        decimal wineGallons,
        int? gaugedByUserId = null,
        string? notes = null)
    {
        // Validate barrel exists
        var barrel = await _context.Barrels.FindAsync(barrelId);
        if (barrel == null)
        {
            throw new ArgumentException($"Barrel with ID {barrelId} not found.", nameof(barrelId));
        }

        // Calculate proof gallons with temperature correction
        var proofGallons = CalculateProofGallons(proof, temperatureFahrenheit, wineGallons);

        var gaugeRecord = new TtbGaugeRecord
        {
            BarrelId = barrelId,
            GaugeDate = DateTime.UtcNow,
            GaugeType = gaugeType,
            Proof = proof,
            Temperature = temperatureFahrenheit,
            WineGallons = wineGallons,
            ProofGallons = proofGallons,
            GaugedByUserId = gaugedByUserId,
            Notes = notes
        };

        _context.TtbGaugeRecords.Add(gaugeRecord);
        await _context.SaveChangesAsync();

        return gaugeRecord;
    }

    public async Task<IEnumerable<TtbGaugeRecord>> GetGaugeRecordsForBarrelAsync(int barrelId)
    {
        return await _context.TtbGaugeRecords
            .Where(g => g.BarrelId == barrelId)
            .Include(g => g.GaugedByUser)
            .OrderByDescending(g => g.GaugeDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<TtbGaugeRecord>> GetGaugeRecordsForCompanyAsync(
        int companyId,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        var query = _context.TtbGaugeRecords
            .Include(g => g.Barrel)
            .Include(g => g.GaugedByUser)
            .Where(g => g.Barrel.CompanyId == companyId);

        if (startDate.HasValue)
        {
            query = query.Where(g => g.GaugeDate >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(g => g.GaugeDate <= endDate.Value);
        }

        return await query
            .OrderByDescending(g => g.GaugeDate)
            .ToListAsync();
    }

    public async Task<TtbGaugeRecord?> GetGaugeRecordByIdAsync(int id)
    {
        return await _context.TtbGaugeRecords
            .Include(g => g.Barrel)
            .Include(g => g.GaugedByUser)
            .FirstOrDefaultAsync(g => g.Id == id);
    }

    public async Task<TtbGaugeRecord> UpdateGaugeRecordAsync(
        int id,
        decimal proof,
        decimal temperatureFahrenheit,
        decimal wineGallons,
        string? notes = null)
    {
        var gaugeRecord = await _context.TtbGaugeRecords.FindAsync(id);
        if (gaugeRecord == null)
        {
            throw new ArgumentException($"Gauge record with ID {id} not found.", nameof(id));
        }

        // Recalculate proof gallons with temperature correction
        var proofGallons = CalculateProofGallons(proof, temperatureFahrenheit, wineGallons);

        gaugeRecord.Proof = proof;
        gaugeRecord.Temperature = temperatureFahrenheit;
        gaugeRecord.WineGallons = wineGallons;
        gaugeRecord.ProofGallons = proofGallons;
        gaugeRecord.Notes = notes;

        await _context.SaveChangesAsync();

        return gaugeRecord;
    }

    public async Task DeleteGaugeRecordAsync(int id)
    {
        var gaugeRecord = await _context.TtbGaugeRecords.FindAsync(id);
        if (gaugeRecord == null)
        {
            throw new ArgumentException($"Gauge record with ID {id} not found.", nameof(id));
        }

        _context.TtbGaugeRecords.Remove(gaugeRecord);
        await _context.SaveChangesAsync();
    }

    private decimal CalculateProofGallons(decimal proof, decimal temperatureFahrenheit, decimal wineGallons)
    {
        // Get temperature correction factor from TTB table
        var correctionFactor = _temperatureCorrectionService.GetCorrectionFactor(temperatureFahrenheit, proof);

        // Calculate proof gallons: wine_gallons * (proof / 100) * correction_factor
        var proofGallons = wineGallons * (proof / 100m) * correctionFactor;

        // Round to 2 decimal places as per TTB requirements
        return Math.Round(proofGallons, 2);
    }
}
