using System;

namespace Caskr.server.Services;

/// <summary>
/// TTB-compliant volume calculations for distilled spirits.
///
/// COMPLIANCE REFERENCE: docs/TTB_FORM_5110_28_MAPPING.md, Section "Proof Gallons Calculation"
/// REGULATORY AUTHORITY: 27 CFR Part 19.1 - Definitions
///
/// CRITICAL: The proof gallon formula is defined by federal law and CANNOT be modified.
/// Proof Gallons = Wine Gallons × (Proof / 100)
/// Where: Proof = ABV × 2
///
/// Example: 100 wine gallons at 62.5% ABV
///   Proof = 62.5 × 2 = 125
///   Proof Gallons = 100 × (125 / 100) = 125.00 PG
///
/// DO NOT use approximations or shortcuts. This calculation is used for federal tax compliance.
/// </summary>
internal static class TtbVolumeCalculator
{
    /// <summary>
    /// Calculates proof gallons from wine gallons and ABV using the official TTB formula.
    /// </summary>
    /// <param name="volumeGallons">Wine gallons (actual liquid volume)</param>
    /// <param name="abv">Alcohol by volume (percentage, e.g., 62.5 for 62.5%)</param>
    /// <returns>Proof gallons rounded to 2 decimal places</returns>
    /// <remarks>
    /// TTB Formula: Proof Gallons = Wine Gallons × (Proof / 100)
    /// Where Proof = ABV × 2
    ///
    /// This is the ONLY correct formula for TTB compliance.
    /// See: docs/TTB_FORM_5110_28_MAPPING.md
    /// </remarks>
    public static decimal CalculateProofGallons(decimal volumeGallons, decimal abv)
    {
        if (volumeGallons <= 0 || abv <= 0)
        {
            return 0m;
        }

        // Proof = ABV × 2 (TTB regulation)
        var proof = abv * 2m;

        // Proof Gallons = Wine Gallons × (Proof / 100)
        return Math.Round(volumeGallons * (proof / 100m), 2, MidpointRounding.AwayFromZero);
    }
}
