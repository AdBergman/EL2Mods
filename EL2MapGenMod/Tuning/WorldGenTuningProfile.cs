namespace EL2MapGenMod.Tuning
{
    internal static class WorldGenTuningProfile
    {
        // ~50% bigger area: 1.25 * 1.25 = 1.5625 (close enough, and stays simple/robust)
        public const int MapScalePercent = 125;

        // Raise land elevation by this amount via WorldGeneratorOptions.StartLandElevation.
        // Sea will automatically start at (StartLandElevation - 1), so land+1 also raises sea+1.
        // This makes the world start one level higher while still leaving water after recessions.
        public const int LandElevationRaise = 1;

        // Ridges / verticality
        public const int MaxLandElevationRaise = 4;
        public const int ElevationGrowPercentBonus = 10;

        public const int RidgePresenceFloor = 75; // percent
        public const int RidgeMinElevationDelta = -1; // slightly easier to qualify as ridge candidate
        public const int RidgeMaxSizeBonus = 2;

        // Rivers
        public const int RiverPresenceFloor = 80; // percent
        public const int RiverSeedCountBonus = 20;
        public const int RiverMinCountBonus = 3;
        public const int RiverMaxCountBonus = 4;
        public const int RiverMaxLengthBonus = 3;
        public const int RiverSourcesMinDistanceDelta = -1;

        // Persistent water rule (after 3rd recession step)
        public const int PersistentWaterMinSeaLevel = 1;
        public const int PersistentWaterClampFromRecessIndex = 3;
    }
}