using System;

namespace Caskr.server.Services;

internal static class TtbVolumeCalculator
{
    public static decimal CalculateProofGallons(decimal volumeGallons, decimal abv)
    {
        if (volumeGallons <= 0 || abv <= 0)
        {
            return 0m;
        }

        var proof = abv * 2m;
        return Math.Round(volumeGallons * (proof / 100m), 2, MidpointRounding.AwayFromZero);
    }
}
