namespace Caskr.server.Services;

/// <summary>
/// Service for applying TTB temperature correction factors to proof gallons calculations.
/// Based on TTB Table 7 - Correction of Volume to 60°F for Distilled Spirits.
/// </summary>
public interface ITtbTemperatureCorrectionService
{
    /// <summary>
    /// Gets the temperature correction factor for a given temperature and proof.
    /// </summary>
    /// <param name="temperatureFahrenheit">Temperature in Fahrenheit</param>
    /// <param name="proof">Proof of the spirits</param>
    /// <returns>Correction factor to apply to volume</returns>
    decimal GetCorrectionFactor(decimal temperatureFahrenheit, decimal proof);
}

public class TtbTemperatureCorrectionService : ITtbTemperatureCorrectionService
{
    // TTB Table 7 - Simplified correction factors
    // In production, this should use the full TTB table with interpolation
    // For now, using a simplified table for proof ranges and temperature ranges

    private static readonly Dictionary<(int tempRange, int proofRange), decimal> CorrectionTable = new()
    {
        // Format: (tempRange, proofRange) -> correction factor
        // Temperature ranges: 0=<40F, 1=40-50F, 2=50-60F, 3=60F, 4=60-70F, 5=70-80F, 6=80-90F, 7=>90F
        // Proof ranges: 0=<100, 1=100-120, 2=120-140, 3=140-160, 4=160-180, 5=>180

        // < 40°F
        {(0, 0), 1.0150m}, {(0, 1), 1.0180m}, {(0, 2), 1.0200m}, {(0, 3), 1.0220m}, {(0, 4), 1.0240m}, {(0, 5), 1.0260m},

        // 40-50°F
        {(1, 0), 1.0100m}, {(1, 1), 1.0120m}, {(1, 2), 1.0135m}, {(1, 3), 1.0150m}, {(1, 4), 1.0165m}, {(1, 5), 1.0180m},

        // 50-60°F
        {(2, 0), 1.0050m}, {(2, 1), 1.0060m}, {(2, 2), 1.0068m}, {(2, 3), 1.0075m}, {(2, 4), 1.0083m}, {(2, 5), 1.0090m},

        // 60°F (standard temperature, no correction)
        {(3, 0), 1.0000m}, {(3, 1), 1.0000m}, {(3, 2), 1.0000m}, {(3, 3), 1.0000m}, {(3, 4), 1.0000m}, {(3, 5), 1.0000m},

        // 60-70°F
        {(4, 0), 0.9950m}, {(4, 1), 0.9940m}, {(4, 2), 0.9933m}, {(4, 3), 0.9925m}, {(4, 4), 0.9918m}, {(4, 5), 0.9910m},

        // 70-80°F
        {(5, 0), 0.9900m}, {(5, 1), 0.9880m}, {(5, 2), 0.9865m}, {(5, 3), 0.9850m}, {(5, 4), 0.9835m}, {(5, 5), 0.9820m},

        // 80-90°F
        {(6, 0), 0.9850m}, {(6, 1), 0.9820m}, {(6, 2), 0.9798m}, {(6, 3), 0.9775m}, {(6, 4), 0.9753m}, {(6, 5), 0.9730m},

        // > 90°F
        {(7, 0), 0.9800m}, {(7, 1), 0.9760m}, {(7, 2), 0.9730m}, {(7, 3), 0.9700m}, {(7, 4), 0.9670m}, {(7, 5), 0.9640m},
    };

    public decimal GetCorrectionFactor(decimal temperatureFahrenheit, decimal proof)
    {
        var tempRange = GetTemperatureRange(temperatureFahrenheit);
        var proofRange = GetProofRange(proof);

        if (CorrectionTable.TryGetValue((tempRange, proofRange), out var factor))
        {
            return factor;
        }

        // Default to no correction if outside ranges
        return 1.0000m;
    }

    private static int GetTemperatureRange(decimal temp)
    {
        return temp switch
        {
            < 40 => 0,
            >= 40 and < 50 => 1,
            >= 50 and < 60 => 2,
            60 => 3,
            > 60 and <= 70 => 4,
            > 70 and <= 80 => 5,
            > 80 and <= 90 => 6,
            _ => 7
        };
    }

    private static int GetProofRange(decimal proof)
    {
        return proof switch
        {
            < 100 => 0,
            >= 100 and < 120 => 1,
            >= 120 and < 140 => 2,
            >= 140 and < 160 => 3,
            >= 160 and < 180 => 4,
            _ => 5
        };
    }
}
