namespace EL2MapGenMod.Patches
{
    // Disabled for Option A.
    // Returning -1 from GetRecessIndexFromElevation can be valid in some places,
    // but it’s also an easy way to crash if any caller assumes a non-negative band.
    internal static class WorldGeneratorContext_GetRecessIndexFromElevation
    {
    }
}