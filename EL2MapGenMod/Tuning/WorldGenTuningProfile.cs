// action: UPDATE
// namespace: EL2MapGenMod.Tuning
// class: WorldGenTuningProfile

namespace EL2MapGenMod.Tuning
{
    internal static class WorldGenTuningProfile
    {
        // ---------------------------------------------------------------------
        // Telemetry / Observability
        // ---------------------------------------------------------------------
        public const bool EnableWorldgenTelemetry = true;
        public const string TelemetryOutputFolder = "EL2MapGenMod";

        // ---------------------------------------------------------------------
        // Canonical rule going forward:
        // ---------------------------------------------------------------------
        public const bool EnforceBottomLayerLakes = true;

        // Safety: avoid converting starting island districts (spawn safety).
        public const bool ProtectStartingIslandsFromBottomLakeConversion = true;

        // Map size scaling:
        // If WidthScale and HeightScale both use this percent, then AREA scales by (p/100)^2.
        public const int MapScalePercent = 112;

        // Land baseline:
        // Vanilla initial sea is (StartLandElevation - 1).
        // GOAL: Land starts at 5, sea at 4.
        public const sbyte StartLandElevationTarget = 5;

        // ---------------------------------------------------------------------
        // Ridges / verticality (global tuning knobs)
        // ---------------------------------------------------------------------
        public const int MaxLandElevationRaise = 4;
        public const int ElevationGrowPercentBonus = 10;

        public const int RidgePresenceFloor = 75; // percent
        public const int RidgeMinElevationDelta = -1; // slightly easier to qualify as ridge candidate
        public const int RidgeMaxSizeBonus = 2;

        // ---------------------------------------------------------------------
        // Rivers
        // ---------------------------------------------------------------------
        public const int RiverPresenceFloor = 80; // percent
        public const int RiverSeedCountBonus = 20;

        public const int RiverMinCountBonus = 3;
        public const int RiverMaxCountBonus = 4;

        public const int RiverMaxLengthBonus = 3;
        public const int RiverSourcesMinDistanceDelta = -1;

        // ---------------------------------------------------------------------
        // Elevation band rebalance (CreateElevations: ridges + lakes)
        // Used by ElevationBandRebalance (DigLakesBanded/SproutRidgesBanded).
        // ---------------------------------------------------------------------

        public const sbyte RidgeStartingBandMinElevation = 3;  // 3-4
        public const sbyte RidgeStartingBandMaxElevation = 4;
        public const int RidgeStartingBandCandidateRejectPercent = 30;

        public const sbyte RidgeIntermediateBandMinElevation = 1; // 1-2
        public const sbyte RidgeIntermediateBandMaxElevation = 3;
        public const int RidgeIntermediateBandPresenceBonusPercent = 15;

        public const sbyte RidgeDeepBandMaxElevation = 0;
        public const int RidgeDeepBandCandidateRejectPercent = 60;

        public const sbyte LakePreferredBandMinElevation = 1; // 1-3
        public const sbyte LakePreferredBandMaxElevation = 3;

        public const sbyte LakeTooLowMaxElevation = 0;
        public const sbyte LakeTooHighMinElevation = 4;
        public const int LakeTooHighRejectPercent = 50;

        // ---------------------------------------------------------------------
        // BOTTOM WATER TARGET (canonical knobs)
        // ---------------------------------------------------------------------

        // Desired % of total map tiles that should end up as bottom-band (Elevation <= 0) water.
        // GOAL: ~30% of the map is permanent bottom layer water for the whole game.
        public const int TargetBottomWaterPercent = 30;

        // If true, ALL lakes (seas + regular lakes) clamp their bottoms to <= 0.
        public const bool ClampAllLakeBottomToZeroOrLower = true;

        // If true, force sea bottoms to be <= 0 (guarantees lowest band water area)
        public const bool ClampInlandSeaBottomToZeroOrLower = true;

        // How deep lakes are dug relative to their surrounding ring.
        // NOTE: old profile used 5; keep as-is unless you intentionally retuned.
        public const int LakeDigDepth = 5;

        // Allow low elevation lake seeds (needed for bottom-band expansion).
        public const bool AllowLowElevationLakeSeeds = true;

        // Prevent seas from touching map edge (keeps them inland-ish).
        public const bool InlandSeasAvoidGridEdge = false;

        // Inland seas: build a few huge lakes instead of many mediums.
        public const int InlandSeaCount = 4;

        // Kept for backward compatibility / debugging; actual per-sea target may be computed from TargetBottomWaterPercent.
        public const int InlandSeaTargetPercentPerSea = 6;

        // After seas are built, do we still allow normal lakes?
        public const bool AllowRegularLakesAfterSeas = true;

        // ---------------------------------------------------------------------
        // Generator-level lake quantity tuning (WorldGeneratorOptions)
        // ---------------------------------------------------------------------

        // Additive boost to WorldGeneratorOptions.LakePresencePercent (byte 0..100).
        public const int LakePresencePercentBonus = 50;

        // Allow smaller lakes by decreasing MinLakeArea (sbyte). Example: 3 -> 2 with -1.
        public const int MinLakeAreaDelta = 0;

        // Optional: shrink MaxLakeArea to avoid mega-lakes and encourage more separate lakes.
        public const int MaxLakeAreaDelta = 2;

        // Optional: loosen MaxCliffDeltaElevation a bit (affects basin candidate validity).
        public const int MaxCliffDeltaElevationDelta = 0;

        // Safety floors.
        public const int MinLakeAreaFloor = 1;
        public const int MaxLakeAreaFloor = 2;

        // ---------------------------------------------------------------------
        // Recession safety
        // ---------------------------------------------------------------------

        // Recess floor: sea will not fall below this elevation.
        // With sea starting at 4, floor=1 allows 4->3->2->1 across 3 recess drops.
        public const int PersistentSeaLevelFloor = 1;
    }
}