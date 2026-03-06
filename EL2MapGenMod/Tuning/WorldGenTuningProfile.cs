namespace EL2MapGenMod.Tuning
{
    internal static class WorldGenTuningProfile
    {
        public const bool EnableWorldgenTelemetry = true;
        public const string TelemetryOutputFolder = "EL2MapGenMod";

        public const int MapScalePercent = 112;
        public const sbyte StartLandElevationTarget = 10;
        public const int ElevationGrowPercentBonus = 45; // Increased to push for 30% water

        public const int MaxLandElevationRaise = 8;
        public const int RidgePresenceFloor = 75;
        public const int RidgeMinElevationDelta = -1;
        public const int RidgeMaxSizeBonus = 2;

        public const int RiverPresenceFloor = 80;
        public const int RiverSeedCountBonus = 20;
        public const int RiverMinCountBonus = 3;
        public const int RiverMaxCountBonus = 4;
        public const int RiverMaxLengthBonus = 3;
        public const int RiverSourcesMinDistanceDelta = -1;

        public const sbyte RidgeStartingBandMinElevation = 6;
        public const sbyte RidgeStartingBandMaxElevation = 7;
        public const int RidgeStartingBandCandidateRejectPercent = 30;

        public const sbyte RidgeIntermediateBandMinElevation = 4;
        public const sbyte RidgeIntermediateBandMaxElevation = 6;
        public const int RidgeIntermediateBandPresenceBonusPercent = 15;

        public const sbyte RidgeDeepBandMaxElevation = 3;
        public const int RidgeDeepBandCandidateRejectPercent = 60;

        public const int LakePresencePercentBonus = 50;
        public const int MinLakeAreaDelta = 0;
        public const int MaxLakeAreaDelta = 2;
        public const int MinLakeAreaFloor = 1;
        public const int MaxLakeAreaFloor = 2;

        public const int PersistentSeaLevelFloor = 3;
        
        
        // ---------------------------------------------------------------------
        // BOTTOM WATER TARGET (canonical knobs)
        // ---------------------------------------------------------------------

        // Every tile at this elevation or lower is forced into bottom-band water.
        public const sbyte BottomWaterMaxElevation = 3;

        // Desired % of total map tiles that should end up as bottom-band water.
        public const int TargetBottomWaterPercent = 30;

        // If true, ALL lakes (seas + regular lakes) clamp their bottoms to <= 0.
        public const bool ClampAllLakeBottomToZeroOrLower = true;

        // If true, force sea bottoms to be <= 0.
        public const bool ClampInlandSeaBottomToZeroOrLower = true;

        public const int LakeDigDepth = 5;
        public const bool AllowLowElevationLakeSeeds = true;
        public const bool InlandSeasAvoidGridEdge = false;
        public const int InlandSeaCount = 4;
        public const int InlandSeaTargetPercentPerSea = 6;
        
        public const bool ProtectStartingIslandsFromBottomLakeConversion = true;
    }
}